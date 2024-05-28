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

using Serilog.Core;
using Serilog.Events;

namespace BsaT53UploadServer.Web.Logging
{
    /// <summary>
    /// Event that is fired when one of the counts in
    /// <see cref="LogMessageCounter"/> gets changed.
    /// </summary>
    /// <param name="newCount"></param>
    public delegate void CountUpdatedEvent( ulong newCount );

    /// <summary>
    /// Keeps track of how many error, warnings, and fatal
    /// messages we get from the log so they can be monitored.
    /// </summary>
    public sealed class LogMessageCounter : ILogEventSink
    {
        // ---------------- Events ----------------

        public event CountUpdatedEvent? WarningCountUpdated;

        public event CountUpdatedEvent? ErrorCountUpdated;

        public event CountUpdatedEvent? FatalCountUpdated;

        // ---------------- Fields ----------------

        private ulong warnings;
        private ulong errors;
        private ulong fatals;

        // ---------------- Constructor ----------------

        public LogMessageCounter()
        {
            this.warnings = 0;
            this.errors = 0;
            this.fatals = 0;
        }

        // ---------------- Properties ----------------

        public ulong WarningsSeen
        {
            get => this.warnings;
            private set
            {
                ++this.warnings;
                this.WarningCountUpdated?.Invoke( this.warnings );
            }
        }

        public ulong ErrorsSeen
        {
            get => this.errors;
            private set
            {
                ++this.errors;
                this.ErrorCountUpdated?.Invoke( this.errors );
            }
        }

        public ulong FatalsSeen
        {
            get => this.fatals;
            private set
            {
                ++this.fatals;
                this.FatalCountUpdated?.Invoke( this.fatals );
            }
        }

        // ---------------- Functions ----------------

        public void Emit( LogEvent logEvent )
        {
            if( logEvent.Level == LogEventLevel.Warning )
            {
                ++this.WarningsSeen;
            }
            else if( logEvent.Level == LogEventLevel.Error )
            {
                ++this.ErrorsSeen;
            }
            else if( logEvent.Level == LogEventLevel.Fatal )
            {
                ++this.FatalsSeen;
            }

            // Otherwise, we don't care about the level.
        }
    }
}
