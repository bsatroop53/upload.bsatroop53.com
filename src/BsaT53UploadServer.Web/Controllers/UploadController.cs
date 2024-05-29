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
using BsaT53UploadServer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using SethCS.Extensions;

namespace BsaT53UploadServer.Web.Controllers
{
    public class UploadController : Controller
    {
        // ---------------- Fields ----------------

        private readonly BsaT53UploadApi api;

        // ---------------- Constructor ----------------

        public UploadController( BsaT53UploadApi api )
        {
            this.api = api;
        }

        // ---------------- Methods ----------------

        public IActionResult Index()
        {
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Index( [FromForm] UploadModel model )
        {
            if( "POST".EqualsIgnoreCase( this.Request.Method ) == false )
            {
                return NotFound();
            }
            else if( model is null )
            {
                return BadRequest( "Model is null" );
            }
            else if( model.File is null )
            {
                return BadRequest( "File is null" );
            }

            string? userAgent = this.Request.Headers.UserAgent;

            this.api.StatusLog.Verbose( "Request User Agent: " + ( userAgent ?? "[null]" ) );

            UploadStatus status = await this.api.TryUpload( model.File, userAgent );
            if( status == UploadStatus.Success )
            {
                return Ok( status.GetErrorMessage() );
            }
            else
            {
                return BadRequest( status.GetErrorMessage() );
            }
        }
    }
}
