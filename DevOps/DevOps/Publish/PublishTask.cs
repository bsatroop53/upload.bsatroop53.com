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

using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.MSBuild;
using Cake.Core.IO;
using Cake.Frosting;
using Seth.CakeLib;

namespace DevOps.Publish
{
    [TaskName( "publish" )]
    public sealed class PublishTask : DevopsTask
    {
        // ---------------- Functions ----------------

        public override void Run( BuildContext context )
        {
            context.EnsureDirectoryExists( context.DistFolder );

            DirectoryPath looseFilesDir = context.LooseFilesDistFolder;
            context.EnsureDirectoryExists( looseFilesDir );
            context.CleanDirectory( looseFilesDir );

            context.Information( "Publishing App" );

            var publishOptions = new DotNetPublishSettings
            {
                Configuration = "Release",
                OutputDirectory = looseFilesDir.ToString(),
                SelfContained = true,
                PublishReadyToRun = true,
                PublishTrimmed = false,
                Runtime = PlatformTarget.x64.ToDotnetRid( PlatformID.Unix ),
                PublishSingleFile = true
            };

            FilePath servicePath = context.SrcDir.CombineWithFilePath(
                "BsaT53UploadServer.Web/BsaT53UploadServer.Web.csproj"
            );

            context.DotNetPublish( servicePath.ToString(), publishOptions );
            context.Information( string.Empty );

            //CopyRootFile( context, "Readme.md" );
            //CopyRootFile( context, "Credits.md" );
            CopyRootFile( context, "LICENSE.md" );
        }

        private static void CopyRootFile( BuildContext context, FilePath fileName )
        {
            fileName = context.RepoRoot.CombineWithFilePath( fileName );
            context.Information( $"Copying '{fileName}' to dist" );
            context.CopyFileToDirectory( fileName, context.LooseFilesDistFolder );
        }
    }
}
