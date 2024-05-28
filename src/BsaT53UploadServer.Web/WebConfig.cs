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

using SethCS.Exceptions;

namespace BsaT53UploadServer.Web
{
    public record class WebConfig
    {
        // ---------------- Properties ----------------

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

        public static WebConfig FromEnvVar()
        {
            bool NotNull( string envName, out string envValue )
            {
                envValue = Environment.GetEnvironmentVariable( envName ) ?? "";
                return string.IsNullOrWhiteSpace( envValue ) == false;
            }

            var settings = new WebConfig();

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

        public static void Validate( this WebConfig config )
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

            if( errors.Any() )
            {
                throw new ListedValidationException(
                    $"Error when validating {nameof( WebConfig )}",
                    errors
                );
            }
        }
    }
}
