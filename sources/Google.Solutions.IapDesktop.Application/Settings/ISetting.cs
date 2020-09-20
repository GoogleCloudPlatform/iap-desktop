using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public interface ISetting
    {
        string Key { get; }
        string Title { get; }
        string Description { get; }
        string Category { get; }
        object Value { get; }
        bool IsDefault { get; }
        bool IsDirty { get; }
        ISetting OverlayBy(ISetting setting);
    }

    public interface ISetting<T> : ISetting
    {
        T DefaultValue { get; }
        ISetting<T> OverlayBy(ISetting<T> setting);
    }
}
