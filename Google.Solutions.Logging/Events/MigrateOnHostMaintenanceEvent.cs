using Google.Solutions.Logging.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Logging.Events
{
    public class MigrateOnHostMaintenanceEvent : VmInstanceEventBase
    {
        public const string Method = "compute.instances.migrateOnHostMaintenance";

        public MigrateOnHostMaintenanceEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsMigrateOnHostMaintenanceEvent(logRecord));
        }

        public static bool IsMigrateOnHostMaintenanceEvent(LogRecord logRecord)
        {
            return logRecord.IsSystemEvent &&
                logRecord.ProtoPayload.MethodName == Method;
        }
    }
}
