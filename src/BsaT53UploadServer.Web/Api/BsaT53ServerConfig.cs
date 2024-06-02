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

using Quartz;
using SethCS.Exceptions;

namespace BsaT53UploadServer.Web.Api
{
    public record class BsaT53ServerConfig
    {
        // ---------------- Constructor ----------------

        public BsaT53ServerConfig()
            : this( AppContext.BaseDirectory )
        {
        }

        public BsaT53ServerConfig( string executingAssemblyLocation )
        {
            this.FileUploadLocation = new DirectoryInfo(
                Path.Combine( executingAssemblyLocation, "t53files" )
            );
        }

        // ---------------- Properties ----------------

        // -------- Server Settings --------

        public DirectoryInfo FileUploadLocation { get; init; }

        /// <summary>
        /// The user agent required in order to upload something.
        /// All other user agents will be denied access.
        /// 
        /// Set to null to allow any user agent through.
        /// </summary>
        public string? T53UploadUserAgent { get; init; } = null;

        /// <summary>
        /// The user agent required in order to check the files that have
        /// been uploaded.  All other user agents will be denied access.
        /// 
        /// Set to null to allow any user agent through.
        /// </summary>
        public string? T53FileCheckUserAgent { get; init; } = null;

        public TimeSpan UploadCoolDownTime { get; init; } = TimeSpan.Zero;

        /// <summary>
        /// The minimum file size in bytes that is allowed to be uploaded.
        /// 0 or less means no limit.
        /// </summary>
        public long MinimumFileSize { get; init; } = 0;

        /// <summary>
        /// The maximum file size in bytes that is allowed to be uploaded.
        /// 0 or less means no limit.
        /// </summary>
        public long MaximumFileSize { get; init; } = 0;

        /// <summary>
        /// File that contains the base 64 string of the OTP key file.
        /// </summary>
        public FileInfo? OtpKeyFile { get; init; } = null;

        /// <summary>
        /// Cron string to enable the maintenance window,
        /// when all requests are ignored.
        /// </summary>
        public string? MaintenanceWindowStart { get; init; } = null;

        /// <summary>
        /// Cron string to disable the maintenance window,
        /// when requests can be processed again.
        /// </summary>
        public string? MaintenanceWindowEnd { get; init; } = null;

        /// <summary>
        /// Cron string to re-read the key specified in <see cref="OtpKeyFile"/>.
        /// </summary>
        public string? ReloadKeyTime { get; init; } = null;

        // -------- Web Settings --------

        /// <summary>
        /// Set this if the service is running not on the root
        /// of the URL.
        /// </summary>
        public string BasePath { get; init; } = "";

        /// <summary>
        /// This should be set to your front-facing URL
        /// (e.g. https://shendrick.net).
        /// 
        /// If the requested URL does not match this, the request will 400.
        /// </summary>
        public Uri BaseUri { get; init; } = new Uri( "http://localhost" );

        /// <summary>
        /// If the given request has a port in
        /// the URL, should we process it?
        /// 
        /// If false, then each request will 400.
        /// </summary>
        public bool AllowPorts { get; init; } = true;

        /// <summary>
        /// If the requested URL that contains "//" this will
        /// set it to "/" instead if true.
        /// </summary>
        public bool RewriteDoubleSlashes { get; init; } = false;

        /// <summary>
        /// The URL that prometheus metrics should be reported to.
        /// Set to null to not report metrics at all.
        /// </summary>
        public string? MetricsUrl { get; init; } = null;

        // -------- Log Settings --------

        /// <summary>
        /// Where to log information or greater messages to.
        /// Leave null for no logging to files.
        /// </summary>
        public FileInfo? LogFile { get; init; } = null;

        public string? TelegramBotToken { get; init; } = null;

        public string? TelegramChatId { get; init; } = null;
    }

    internal static class WebConfigExtensions
    {
        // ---------------- Functions ----------------

        public static BsaT53ServerConfig FromEnvVar()
        {
            bool NotNull( string envName, out string envValue )
            {
                envValue = Environment.GetEnvironmentVariable( envName ) ?? "";
                return string.IsNullOrWhiteSpace( envValue ) == false;
            }

            var settings = new BsaT53ServerConfig();

            if( NotNull( "T53_FILE_STAGING_DIRECTORY", out string stagingDir ) )
            {
                settings = settings with
                {
                    FileUploadLocation = new DirectoryInfo( stagingDir )
                };
            }

            if( NotNull( "T53_UPLOAD_USER_AGENT", out string uploadUserAgent ) )
            {
                settings = settings with
                {
                    T53UploadUserAgent = uploadUserAgent
                };
            }

            if( NotNull( "T53_FILE_CHECK_USER_AGENT", out string fileCheckUserAgent ) )
            {
                settings = settings with
                {
                    T53FileCheckUserAgent = fileCheckUserAgent
                };
            }

            if( NotNull( "T53_UPLOAD_COOLDOWN_TIME", out string coolDownStr ) )
            {
                int minutes = int.Parse( coolDownStr );
                settings = settings with
                {
                    UploadCoolDownTime = TimeSpan.FromMinutes( minutes )
                };
            }

            if( NotNull( "T53_MIN_FILE_SIZE", out string minFileSizeStr ) )
            {
                long bytes = long.Parse( minFileSizeStr );
                settings = settings with
                {
                    MinimumFileSize = bytes
                };
            }

            if( NotNull( "T53_MAX_FILE_SIZE", out string maxFileSizeStr ) )
            {
                long bytes = long.Parse( maxFileSizeStr );
                settings = settings with
                {
                    MaximumFileSize = bytes
                };
            }

            if( NotNull( "T53_OTP_KEY_FILE", out string otpKey ) )
            {
                settings = settings with 
                {
                    OtpKeyFile = new FileInfo( otpKey )
                };
            }

            if( NotNull( "T53_START_MAINTENANCE", out string startMaintenanceStr ) )
            {
                settings = settings with
                {
                    MaintenanceWindowStart = startMaintenanceStr
                };
            }

            if( NotNull( "T53_END_MAINTENANCE", out string endMaintenanceStr ) )
            {
                settings = settings with
                {
                    MaintenanceWindowEnd = endMaintenanceStr
                };
            }

            if( NotNull( "T53_RELOAD_KEY", out string reloadKeyStr ) )
            {
                settings = settings with
                {
                    ReloadKeyTime = reloadKeyStr
                };
            }

            if( NotNull( "WEB_ALLOW_PORTS", out string allowPorts ) )
            {
                settings = settings with
                {
                    AllowPorts = bool.Parse( allowPorts )
                };
            }

            if( NotNull( "WEB_BASE_PATH", out string basePath ) )
            {
                settings = settings with
                {
                    BasePath = basePath
                };
            }

            if( NotNull( "WEB_BASE_URI", out string baseUrl ) )
            {
                settings = settings with
                {
                    BaseUri = new Uri( baseUrl )
                };
            }

            if( NotNull( "WEB_METRICS_URL", out string metricsUrl ) )
            {
                settings = settings with
                {
                    MetricsUrl = metricsUrl
                };
            }

            if( NotNull( "WEB_STRIP_DOUBLE_SLASH", out string stripDoubleSlash ) )
            {
                settings = settings with
                {
                    RewriteDoubleSlashes = bool.Parse( stripDoubleSlash )
                };
            }

            if( NotNull( "LOG_FILE", out string logFile ) )
            {
                settings = settings with { LogFile = new FileInfo( logFile ) };
            }

            if( NotNull( "LOG_TELEGRAM_BOT_TOKEN", out string tgBotToken ) )
            {
                settings = settings with { TelegramBotToken = tgBotToken };
            }

            if( NotNull( "LOG_TELEGRAM_CHAT_ID", out string tgChatId ) )
            {
                settings = settings with { TelegramChatId = tgChatId };
            }

            return settings;
        }

        public static void Validate( this BsaT53ServerConfig config )
        {
            var errors = new List<string>();

            if( config.MetricsUrl is not null )
            {
                if( config.MetricsUrl.StartsWith( '/' ) == false )
                {
                    errors.Add( $"{nameof( config.MetricsUrl )} must start with a '/'." );
                }
                else if( config.MetricsUrl.Length <= 1 )
                {
                    errors.Add( $"{nameof( config.MetricsUrl )} must be 2 or greater characters.  Got: {config.MetricsUrl}" );
                }
            }

            if( config.BaseUri is null )
            {
                errors.Add( $"{nameof( config.BaseUri )} must be specified." );
            }

            if( config.OtpKeyFile?.Exists == false )
            {
                errors.Add( $"{config.OtpKeyFile.FullName} file does not exist." );
            }

            if( config.MaintenanceWindowStart is not null )
            {
                if( CronExpression.IsValidExpression( config.MaintenanceWindowStart ) == false )
                {
                    errors.Add( $"{nameof( config.MaintenanceWindowStart )} is invalid cron string: {config.MaintenanceWindowStart}" );
                }
            }

            if( config.MaintenanceWindowEnd is not null )
            {
                if( CronExpression.IsValidExpression( config.MaintenanceWindowEnd ) == false )
                {
                    errors.Add( $"{nameof( config.MaintenanceWindowEnd )} is invalid cron string: {config.MaintenanceWindowEnd}" );
                }
            }

            if( config.ReloadKeyTime is not null )
            {
                if( CronExpression.IsValidExpression( config.ReloadKeyTime ) == false )
                {
                    errors.Add( $"{nameof( config.ReloadKeyTime )} is invalid cron string: {config.ReloadKeyTime}" );
                }
            }

            if( errors.Any() )
            {
                throw new ListedValidationException(
                    $"Error when validating {nameof( BsaT53ServerConfig )}",
                    errors
                );
            }
        }
    }
}
