//
// BSA Troop 53 Upload Server
// Copyright (C) 2024 Seth Hendrick
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using BsaT53UploadServer.Web.Api;
using BsaT53UploadServer.Web.Logging;
using dotenv.net;
using Microsoft.AspNetCore.HttpOverrides;
using Mono.Options;
using Prometheus;
using Serilog;
using Serilog.Sinks.Telegram.Alternative;

namespace BsaT53UploadServer.Web
{
    public sealed class WebBuilder
    {
        // ---------------- Fields ----------------

        private readonly string[] args;

        /// <summary>
        /// Log that prints statuses.
        /// </summary>
        /// <remarks>
        /// This is null until <see cref="Run"/> is called.
        /// </remarks>
        private Serilog.ILogger? statusLog;

        /// <summary>
        /// Log that notifies someone when a thing gets uploaded.
        /// </summary>
        /// <remarks>
        /// This is null until <see cref="Run"/> is called.
        /// </remarks>
        private Serilog.ILogger? notificationLog;

        /// <remarks>
        /// This is null until <see cref="Run"/> is called.
        /// </remarks>
        private ServerMetrics? metrics;

        // ---------------- Constructor ----------------

        public WebBuilder( string[] args )
        {
            this.args = args;
        }

        // ---------------- Functions ----------------

        public int Run()
        {
            bool showHelp = false;
            bool showVersion = false;
            bool showLicense = false;
            bool showCredits = false;
            string envFile = string.Empty;

            var options = new OptionSet
            {
                {
                    "h|help",
                    "Shows thie mesage and exits.",
                    v => showHelp = ( v is not null )
                },
                {
                    "version",
                    "Shows the version and exits.",
                    v => showVersion = ( v is not null )
                },
                {
                    "print_license",
                    "Prints the software license and exits.",
                    v => showLicense = ( v is not null )
                },
                {
                    "print_credits",
                    "Prints the third-party notices and credits.",
                    v => showCredits = ( v is not null )
                },
                {
                    "env=",
                    "The .env file that contains the environment variable settings.",
                    v => envFile = v
                }
            };

            try
            {
                options.Parse( args );

                if( showHelp )
                {
                    options.WriteOptionDescriptions( Console.Out );
                    return 0;
                }
                else if( showVersion )
                {
                    PrintVersion();
                    return 0;
                }
                else if( showLicense )
                {
                    PrintLicense();
                    return 0;
                }
                else if( showCredits )
                {
                    PrintCredits();
                    return 0;
                }

                options.Parse( args );

                if( string.IsNullOrWhiteSpace( envFile ) == false )
                {
                    Console.WriteLine( $"Using .env file located at '{envFile}'" );
                    DotEnv.Load( new DotEnvOptions( envFilePaths: new string[] { envFile } ) );
                }

                RunInternal();

                this.statusLog?.Information( "Application Exiting" );
                return 0;
            }
            catch( Exception e )
            {
                this.statusLog?.Fatal( "FATAL ERROR:" + Environment.NewLine + e );
                return -1;
            }
            finally
            {
                this.metrics?.Dispose();
            }
        }

        private void RunInternal()
        {
            BsaT53ServerConfig webConfig = WebConfigExtensions.FromEnvVar();

            LogMessageCounter? logCounter = null;
            if( webConfig.MetricsUrl is not null )
            {
                logCounter = new LogMessageCounter();
                this.metrics = new ServerMetrics( logCounter );
            }

            this.statusLog = CreateLog(
                webConfig,
                logCounter,
                Serilog.Events.LogEventLevel.Warning,
                true
            );
            this.notificationLog = CreateLog(
                webConfig,
                logCounter,
                Serilog.Events.LogEventLevel.Information,
                false
            );

            using BsaT53UploadApi api = new BsaT53UploadApi( this.statusLog, this.notificationLog, webConfig );
            api.Init();

            WebApplicationBuilder builder = WebApplication.CreateBuilder( args );
            builder.Services.AddSingleton( webConfig );
            builder.Services.AddSingleton( api );

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Host.UseSerilog( this.statusLog );

            WebApplication app = builder.Build();

            app.UseCors(
                builder => builder
                .WithOrigins( "http://localhost", "http://127.0.0.1", "https://edit.bsatroop53.com" )
                .WithMethods( "GET", "OPTIONS", "POST" )
            );

            if( string.IsNullOrWhiteSpace( webConfig.BasePath ) == false )
            {
                app.Use(
                    ( HttpContext context, RequestDelegate next ) =>
                    {
                        context.Request.PathBase = webConfig.BasePath;
                        return next( context );
                    }
                );
            }

            if( webConfig.RewriteDoubleSlashes )
            {
                app.Use( ( context, next ) =>
                {
                    string? value = context.Request.Path.Value;
                    if( ( value is not null ) && value.StartsWith( "//" ) )
                    {
                        context.Request.Path = new PathString( value.Replace( "//", "/" ) );
                    }
                    return next();
                } );
            }

            app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                }
            );

            if( webConfig.AllowPorts == false )
            {
                app.Use(
                    ( HttpContext context, RequestDelegate next ) =>
                    {
                        int? port = context.Request.Host.Port;
                        if( port is not null )
                        {
                            // Kill the connection,
                            // and stop all processing.
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            context.Connection.RequestClose();
                            return Task.CompletedTask;
                        }

                        return next( context );
                    }
                );
            }

            app.UseHostFiltering();

            // Configure the HTTP request pipeline.
            if( !app.Environment.IsDevelopment() )
            {
                app.UseExceptionHandler( "/Home/Error" );
            }

            app.UseRouting();
            if( webConfig.MetricsUrl is not null )
            {
                // Per https://learn.microsoft.com/en-us/aspnet/core/diagnostics/asp0014?view=aspnetcore-8.0:
                // Warnings from this rule can be suppressed if
                // the target UseEndpoints invocation is invoked without
                // any mappings as a strategy to organize middleware ordering.
#pragma warning disable ASP0014 // Suggest using top level route registrations
                app.UseEndpoints(
                    endpoints =>
                    {
                        endpoints.MapMetrics( webConfig.MetricsUrl );
                    }
                );
#pragma warning restore ASP0014 // Suggest using top level route registrations
            }
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            app.Run();
        }

        private Serilog.ILogger CreateLog(
            BsaT53ServerConfig webConfig,
            LogMessageCounter? logCounter,
            Serilog.Events.LogEventLevel telegramEventLevel,
            bool printEnabledLogs
        )
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console( Serilog.Events.LogEventLevel.Information );

            if( logCounter is not null )
            {
                logger = logger.WriteTo.Sink( logCounter );
            }

            bool useFileLogger = false;
            bool useTelegramLogger = false;

            FileInfo? logFile = webConfig.LogFile;
            if( logFile is not null )
            {
                useFileLogger = true;
                logger.WriteTo.File(
                    logFile.FullName,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                    retainedFileCountLimit: 10,
                    fileSizeLimitBytes: 512 * 1000 * 1000, // 512 MB
                    shared: false
                );
            }

            string? telegramBotToken = webConfig.TelegramBotToken;
            string? telegramChatId = webConfig.TelegramChatId;
            if(
                ( string.IsNullOrWhiteSpace( telegramBotToken ) == false ) &&
                ( string.IsNullOrWhiteSpace( telegramChatId ) == false )
            )
            {
                useTelegramLogger = true;
                var telegramOptions = new TelegramSinkOptions(
                    botToken: telegramBotToken,
                    chatId: telegramChatId,
                    dateFormat: "dd.MM.yyyy HH:mm:sszzz",
                    applicationName: $"{nameof( BsaT53UploadServer )}",
                    failureCallback: this.OnTelegramFailure
                );
                logger.WriteTo.Telegram(
                    telegramOptions,
                    restrictedToMinimumLevel: telegramEventLevel
                );
            }

            Serilog.ILogger log = logger.CreateLogger();
            if( printEnabledLogs )
            {
                log.Information( $"Using File Logging: {useFileLogger}." );
                log.Information( $"Using Telegram Logging: {useTelegramLogger}." );
            }

            return log;
        }

        private void OnTelegramFailure( Exception e )
        {
            this.statusLog?.Warning( $"Telegram message did not send:{Environment.NewLine}{e}" );
        }

        private static void PrintCredits()
        {
            Console.WriteLine( Resources.GetCredits() );
        }

        private static void PrintLicense()
        {
            Console.WriteLine( Resources.GetLicense() );
        }

        private static void PrintVersion()
        {
            Console.WriteLine( GetVersion() );
        }

        private static string GetVersion()
        {
            return typeof( WebBuilder ).Assembly.GetName().Version?.ToString( 3 ) ?? "Unknown Version";
        }
    }
}
