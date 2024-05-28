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

using System.Diagnostics;
using System.Text;
using BsaT53UploadServer.Web.Api;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BsaT53UploadServer.Tests
{
    [TestClass]
    public sealed class UploadFileTests
    {
        // ---------------- Fields ----------------

        private const string zipExtension = ".zip.bsat53";

        private const string mdExtension = ".md.bsat53";

        private Mock<Serilog.ILogger>? statusLog;
        private Mock<Serilog.ILogger>? notificationLog;

        // ---------------- Setup / Teardown ----------------

        [TestInitialize]
        public void TestSetup()
        {
            this.statusLog = new Mock<Serilog.ILogger>( MockBehavior.Loose );
            this.notificationLog = new Mock<Serilog.ILogger>( MockBehavior.Loose );
        }

        [TestCleanup]
        public void TestTeardown()
        {
            this.statusLog = null;
            this.notificationLog = null;
        }

        // ---------------- Tests ----------------

        /// <summary>
        /// Ensures if there are no restrictions on the config
        /// (default settings), the file upload just works
        /// with a zip file.
        /// </summary>
        [TestMethod]
        public void NoRestrictionZipFileSuccessTest()
        {
            var config = new BsaT53ServerConfig();

            const string fileName = nameof( NoRestrictionZipFileSuccessTest ) + zipExtension;

            ExpectSuccessTest( config, fileName, null );
        }

        /// <summary>
        /// Ensures if there are no restrictions on the config
        /// (default settings), the file upload just works
        /// with a zip file.
        /// </summary>
        [TestMethod]
        public void NoRestrictionMdFileSuccessTest()
        {
            var config = new BsaT53ServerConfig();

            const string fileName = nameof( NoRestrictionMdFileSuccessTest ) + mdExtension;

            ExpectSuccessTest( config, fileName, null );
        }

        /// <summary>
        /// If we expect a user agent of some kind,
        /// but none is on the client, we expect an error status.
        /// </summary>
        [TestMethod]
        public void NullClientUserAgentTest()
        {
            // Setup
            var config = new BsaT53ServerConfig
            {
                T53UploadUserAgent = "Some Agent"
            };

            var file = new Mock<IFormFile>( MockBehavior.Strict );

            using BsaT53UploadApi uut = new BsaT53UploadApi( this.statusLog?.Object, this.notificationLog?.Object, config );

            // Act
            UploadStatus status = uut.TryUpload( file.Object, null ).Result;

            // Check
            Assert.AreEqual( UploadStatus.InvalidUserAgent, status );
        }

        /// <summary>
        /// If the client user agent does not match our expected user
        /// agent, we expect an error status.
        /// </summary>
        [TestMethod]
        public void MismatchedUserAgentTest()
        {
            // Setup
            var config = new BsaT53ServerConfig
            {
                T53UploadUserAgent = "Some Agent"
            };

            var file = new Mock<IFormFile>( MockBehavior.Strict );

            using BsaT53UploadApi uut = new BsaT53UploadApi( this.statusLog?.Object, this.notificationLog?.Object, config );

            // Act
            UploadStatus status = uut.TryUpload( file.Object, "A different agent" ).Result;

            // Check
            Assert.AreEqual( UploadStatus.InvalidUserAgent, status );
        }

        /// <summary>
        /// Ensures if the user agent is correct, the file gets uploaded
        /// correctly.
        /// </summary>
        [TestMethod]
        public void CorrectUserAgentTest()
        {
            var config = new BsaT53ServerConfig
            {
                T53UploadUserAgent = "Some Agent"
            };

            const string fileName = nameof( CorrectUserAgentTest ) + zipExtension;

            ExpectSuccessTest( config, fileName, config.T53UploadUserAgent );
        }

        [TestMethod]
        public void MissingT53ExtensionFromZipFileExtensionTest()
        {
            // Setup
            var config = new BsaT53ServerConfig();

            const string fileName = nameof( MissingT53ExtensionFromZipFileExtensionTest ) + ".zip";

            var file = new Mock<IFormFile>( MockBehavior.Strict );
            file.Setup( m => m.FileName ).Returns( fileName );

            using BsaT53UploadApi uut = new BsaT53UploadApi( this.statusLog?.Object, this.notificationLog?.Object, config );

            // Act
            UploadStatus status = uut.TryUpload( file.Object, null ).Result;

            // Check
            Assert.AreEqual( UploadStatus.InvalidFileExtension, status );
        }

        [TestMethod]
        public void MissingT53ExtensionFromMdFileExtensionTest()
        {
            // Setup
            var config = new BsaT53ServerConfig();

            const string fileName = nameof( MissingT53ExtensionFromMdFileExtensionTest ) + ".md";

            var file = new Mock<IFormFile>( MockBehavior.Strict );
            file.Setup( m => m.FileName ).Returns( fileName );

            using BsaT53UploadApi uut = new BsaT53UploadApi( this.statusLog?.Object, this.notificationLog?.Object, config );

            // Act
            UploadStatus status = uut.TryUpload( file.Object, null ).Result;

            // Check
            Assert.AreEqual( UploadStatus.InvalidFileExtension, status );
        }

        [TestMethod]
        public void InvalidSubExtensionTest()
        {
            // Setup
            var config = new BsaT53ServerConfig();

            const string fileName = nameof( InvalidSubExtensionTest ) + ".txt.bsat53";

            var file = new Mock<IFormFile>( MockBehavior.Strict );
            file.Setup( m => m.FileName ).Returns( fileName );

            using BsaT53UploadApi uut = new BsaT53UploadApi( this.statusLog?.Object, this.notificationLog?.Object, config );

            // Act
            UploadStatus status = uut.TryUpload( file.Object, null ).Result;

            // Check
            Assert.AreEqual( UploadStatus.InvalidFileType, status );
        }

        [TestMethod]
        public void FileTooBigTest()
        {
            // Setup
            const string fileContents = "123";
            var config = new BsaT53ServerConfig
            {
                MaximumFileSize = fileContents.Length - 1
            };

            const string fileName = nameof( FileTooBigTest ) + zipExtension;

            var file = new Mock<IFormFile>( MockBehavior.Strict );
            file.Setup( m => m.FileName ).Returns( fileName );
            file.Setup( m => m.Length ).Returns( fileContents.Length );

            using BsaT53UploadApi uut = new BsaT53UploadApi( this.statusLog?.Object, this.notificationLog?.Object, config );

            // Act
            UploadStatus status = uut.TryUpload( file.Object, null ).Result;

            // Check
            Assert.AreEqual( UploadStatus.FileTooBig, status );
        }

        [TestMethod]
        public void FileEqualToLimitTest()
        {
            // Setup
            const string fileContents = "123";
            var config = new BsaT53ServerConfig
            {
                MaximumFileSize = fileContents.Length
            };

            const string fileName = nameof( FileEqualToLimitTest ) + zipExtension;

            ExpectSuccessTest(
                config,
                fileName,
                null,
                fileContents
            );
        }

        [TestMethod]
        public void FileLessThanLimitTest()
        {
            // Setup
            const string fileContents = "123";
            var config = new BsaT53ServerConfig
            {
                MaximumFileSize = fileContents.Length + 1
            };

            const string fileName = nameof( FileLessThanLimitTest ) + zipExtension;

            ExpectSuccessTest(
                config,
                fileName,
                null,
                fileContents
            );
        }

        // ---------------- Test Helpers ----------------

        private void ExpectSuccessTest(
            BsaT53ServerConfig config,
            string fileName,
            string? clientUserAgent,
            string? fileContents = null
        )
        {
            // Setup
            string? location = Path.GetDirectoryName( GetType().Assembly.Location );
            Assert.IsNotNull( location );
            config = config with
            {
                FileUploadLocation = new DirectoryInfo( location )
            };

            long timeStamp = Stopwatch.GetTimestamp();

            var expectedFile = new FileInfo(
                Path.Combine( config.FileUploadLocation.FullName, timeStamp + "_" + fileName )
            );

            string text = fileContents ?? timeStamp.ToString();

            using var readStream = new MemoryStream(
                Encoding.UTF8.GetBytes( text )
            );

            var file = new Mock<IFormFile>( MockBehavior.Strict );
            file.Setup( m => m.FileName ).Returns( fileName );
            file.Setup( m => m.Length ).Returns( text.Length );
            file.Setup( m => m.OpenReadStream() ).Returns( readStream );

            using BsaT53UploadApi uut = new BsaT53UploadApi( this.statusLog?.Object, this.notificationLog?.Object, config );

            try
            {
                if( expectedFile.Exists )
                {
                    File.Delete( expectedFile.FullName );
                }

                // Act
                UploadStatus status = uut.TryUpload( file.Object, clientUserAgent, () => timeStamp ).Result;

                // Check
                Assert.AreEqual( UploadStatus.Success, status );

                expectedFile.Refresh();
                Assert.IsTrue( expectedFile.Exists );
                Assert.AreEqual( text, File.ReadAllText( expectedFile.FullName ) );
            }
            finally
            {
                expectedFile.Refresh();
                if( expectedFile.Exists )
                {
                    File.Delete( expectedFile.FullName );
                }
            }
        }
    }
}
