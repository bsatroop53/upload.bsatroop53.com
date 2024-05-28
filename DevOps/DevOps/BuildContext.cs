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

using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace DevOps
{
    public class BuildContext : FrostingContext
    {
        // ---------------- Constructor ----------------

        public BuildContext( ICakeContext context ) :
            base( context )
        {
            this.RepoRoot = context.Environment.WorkingDirectory;
            this.SrcDir = this.RepoRoot.Combine( "src" );
            this.Solution = this.SrcDir.CombineWithFilePath( "BsaT53UploadServer.sln" );
            this.DistFolder = this.RepoRoot.Combine( "dist" );
            this.LooseFilesDistFolder = this.DistFolder.Combine( "files" );
            this.ZipFilesDistFolder = this.DistFolder.Combine( "zip" );
            this.TestResultsFolder = this.RepoRoot.Combine( "TestResults" );
            this.WebCsProj = this.SrcDir.CombineWithFilePath( "BsaT53UploadServer.Web/BsaT53UploadServer.Web.csproj" );
            this.TestCsProj = this.SrcDir.CombineWithFilePath( "BsaT53UploadServer.Tests/BsaT53UploadServer.Tests.csproj" );
        }

        // ---------------- Properties ----------------

        public DirectoryPath RepoRoot { get; }

        public DirectoryPath SrcDir { get; }

        public FilePath Solution { get; }

        public DirectoryPath DistFolder { get; }

        public DirectoryPath LooseFilesDistFolder { get; }

        public DirectoryPath TestResultsFolder { get; }

        public DirectoryPath ZipFilesDistFolder { get; }

        public FilePath WebCsProj { get; }
        
        public FilePath TestCsProj { get; }
    }
}
