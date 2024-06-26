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
using OtpNet;

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

        private Totp? otpGenerator;

        private bool isInMaintenanceMode;

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
            this.IsInMaintenanceMode = false;

            this.startTime = Stopwatch.GetTimestamp();
        }

        // ---------------- Properties ----------------

        public bool IsInMaintenanceMode
        {
            get => this.isInMaintenanceMode;
            set
            {
                if( value == this.isInMaintenanceMode ) 
                {
                    return;
                }

                this.isInMaintenanceMode = value;

                if( this.isInMaintenanceMode )
                {
                    this.StatusLog.Information( "Maintenance Mode Enabled." );
                }
                else
                {
                    this.StatusLog.Information( "Maintenance Mode Disabled." );
                }
            }
        }

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

            RegenerateKey();
        }

        public void RegenerateKey()
        {
            if( config.OtpKeyFile is not null )
            {
                this.StatusLog.Information( "Regenerating OTP Key" );

                string otpKey = File.ReadAllText( this.config.OtpKeyFile.FullName ).Trim();

                byte[] key = Convert.FromBase64String( otpKey );
                this.otpGenerator = new Totp(
                    key,
                    step: 30,
                    mode: OtpHashMode.Sha512,
                    totpSize: 8,
                    timeCorrection: null
                );
            }
        }

        public async Task<UploadStatus> TryUpload( IFormFile file, string? userAgent, string? otpCode )
        {
            return await TryUpload( file, userAgent, otpCode, Stopwatch.GetTimestamp );
        }

        public async Task<UploadStatus> TryUpload( IFormFile file, string? userAgent, string? otpCode, Func<long> getTimeStamp )
        {
            if( this.IsInMaintenanceMode )
            {
                return UploadStatus.DownForMaintenance;
            }

            if( this.otpGenerator is not null )
            {
                if( otpCode is null )
                {
                    return UploadStatus.MissingKey;
                }
                bool keyMatch = this.otpGenerator.VerifyTotp( otpCode, out long _, VerificationWindow.RfcSpecifiedNetworkDelay );

                if( keyMatch == false )
                {
                    return UploadStatus.InvalidKey;
                }
            }

            if( this.config.T53UploadUserAgent is not null )
            {
                if( userAgent != this.config.T53UploadUserAgent )
                {
                    return UploadStatus.InvalidUserAgent;
                }
            }

            if( this.config.MinimumFileSize > 0 )
            {
                if( file.Length < this.config.MinimumFileSize )
                {
                    return UploadStatus.FileTooSmall;
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

            try
            {
                using( Stream iStream = file.OpenReadStream() )
                {
                    using( Stream oStream = new FileStream( fullPath, FileMode.OpenOrCreate, FileAccess.Write ) )
                    {
                        await iStream.CopyToAsync( oStream );
                    }
                }
            }
            finally
            {
                if( OperatingSystem.IsLinux() && File.Exists( fullPath ) )
                {
                    File.SetUnixFileMode( fullPath, UnixFileMode.UserRead );
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
