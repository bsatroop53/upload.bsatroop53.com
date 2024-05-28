//
// Kakama - An ActivityPub Bot Framework
// Copyright (C) 2023-2024 Seth Hendrick
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
using BsaT53UploadServer.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BsaT53UploadServer.Web.Controllers
{
    public class HomeController : Controller
    {
        // ---------------- Constructor ----------------

        public HomeController()
        {
        }

        // ---------------- Functions ----------------

        [Route( "/" )]
        public IActionResult Index()
        {
            return View( new HomeModel() );
        }

        [Route( "/license.html" )]
        public IActionResult License()
        {
            return View( new HomeModel() );
        }

        [Route( "/credits.html" )]
        public IActionResult Credits()
        {
            return View( new HomeModel() );
        }

        [ResponseCache( Duration = 0, Location = ResponseCacheLocation.None, NoStore = true )]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                }
            );
        }
    }
}