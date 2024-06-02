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

namespace BsaT53UploadServer.Web.Api
{
    public enum UploadStatus
    {
        Success = 0,

        InvalidUserAgent = 1,

        FileTooBig = 2,

        InvalidFileExtension = 3,

        InvalidFileType = 4,

        InvalidFileName = 5,

        InvalidKey = 6,

        MissingKey = 7,

        FileTooSmall = 8,

        DownForMaintenance = 9
    }

    public static class UploadStatusExtensions
    {
        public static string GetErrorMessage( this UploadStatus status )
        {
            switch ( status )
            {
                case UploadStatus.Success:
                    return "File Uploaded Successfully!";

                case UploadStatus.InvalidUserAgent:
                    return "Invalid User Agent.";

                case UploadStatus.FileTooBig:
                    return "File is too big.";

                case UploadStatus.InvalidFileExtension:
                    return "Invalid File.";

                case UploadStatus.InvalidFileType:
                    return "Invalid File Type.";

                case UploadStatus.InvalidFileName:
                    return "Invalid File Name.";

                case UploadStatus.MissingKey:
                    return "Key Not Found.";

                case UploadStatus.InvalidKey:
                    return "Invalid Key. Check your PC's clock and make sure its set correctly.";

                case UploadStatus.FileTooSmall:
                    return "File is too small.";

                case UploadStatus.DownForMaintenance:
                    return "Site is down for maintenance.  Please try again later.";

                default:
                    return "Unknown Error!";
            }
        }
    }
}
