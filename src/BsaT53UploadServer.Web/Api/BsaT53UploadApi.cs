﻿//
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

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BsaT53UploadServer.Web.Api
{
    public sealed class BsaT53UploadApi : IDisposable
    {
        // ---------------- Fields ----------------

        private static readonly Regex invalidCharacters = new Regex(
            @"/\\",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        private readonly BsaT53ServerConfig config;

        private readonly long startTime;

        // ---------------- Constructor ----------------

        public BsaT53UploadApi(
            Serilog.ILogger? statusLog,
            Serilog.ILogger? notificationLog,
            BsaT53ServerConfig config
        )
        {
            ArgumentNullException.ThrowIfNull( statusLog );
            ArgumentNullException.ThrowIfNull( notificationLog );

            this.StatusLog = statusLog;
            this.NotificationLog = notificationLog;
            this.config = config;

            this.startTime = Stopwatch.GetTimestamp();
        }

        // ---------------- Properties ----------------

        public Serilog.ILogger StatusLog { get; }

        public Serilog.ILogger NotificationLog { get; }

        // ---------------- Methods ----------------

        public void Init()
        {
            this.StatusLog.Information( "Initializing API" );

            if( this.config.FileUploadLocation.Exists == false )
            {
                this.StatusLog.Information( $"Creating staging directory at {this.config.FileUploadLocation.FullName}" );
                Directory.CreateDirectory( this.config.FileUploadLocation.FullName );
            }
        }

        public async Task<UploadStatus> TryUpload( IFormFile file, string? userAgent )
        {
            return await TryUpload( file, userAgent, Stopwatch.GetTimestamp );
        }

        public async Task<UploadStatus> TryUpload( IFormFile file, string? userAgent, Func<long> getTimeStamp )
        {
            if( this.config.T53UploadUserAgent is not null )
            {
                if( userAgent != this.config.T53UploadUserAgent )
                {
                    return UploadStatus.InvalidUserAgent;
                }
            }

            if( this.config.MaximumFileSize > 0 )
            {
                if( file.Length > this.config.MaximumFileSize )
                {
                    return UploadStatus.FileTooBig;
                }
            }

            string fileName = file.FileName;
            if( Path.GetExtension( fileName ) != ".bsat53" )
            {
                return UploadStatus.InvalidFileExtension;
            }

            string trueFileName = Path.GetFileNameWithoutExtension( fileName );
            string trueExtension = Path.GetExtension( trueFileName );
            if( ( trueExtension != ".zip" ) && ( trueExtension != ".md" ) )
            {
                return UploadStatus.InvalidFileType;
            }

            string newFileName = $"{getTimeStamp()}_{file.FileName}";

            if( invalidCharacters.IsMatch( newFileName ) )
            {
                return UploadStatus.InvalidFileName;
            }

            string fullPath = Path.Combine( this.config.FileUploadLocation.FullName, newFileName );

            using( Stream iStream = file.OpenReadStream() )
            {
                using( Stream oStream = new FileStream( fullPath, FileMode.OpenOrCreate, FileAccess.Write ) )
                {
                    await iStream.CopyToAsync( oStream );
                }
            }

            this.NotificationLog.Information( $"{file.FileName} has been uploaded as {newFileName}!" );

            return UploadStatus.Success;
        }

        public void Dispose()
        {
            this.StatusLog.Information( "Disposing API" );
        }
    }
}
