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
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Index( [FromForm] IFormFile file )
        {
            if( this.Request.Method != "POST" )
            {
                return NotFound();
            }

            string? userAgent = this.Request.Headers.UserAgent;

            UploadStatus status = await this.api.TryUpload( file, userAgent );
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
