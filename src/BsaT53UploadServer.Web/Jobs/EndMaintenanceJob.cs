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
using Quartz;

namespace BsaT53UploadServer.Web.Jobs
{
    public sealed class EndMaintenanceJob : IJob
    {
        // ---------------- Fields ----------------

        private readonly BsaT53UploadApi api;

        // ---------------- Constructor ----------------

        public EndMaintenanceJob( BsaT53UploadApi api )
        {
            this.api = api;
        }

        // ---------------- Methods ----------------

        public async Task Execute( IJobExecutionContext context )
        {
            this.api.IsInMaintenanceMode = false;
            await Task.CompletedTask;
        }
    }
}
