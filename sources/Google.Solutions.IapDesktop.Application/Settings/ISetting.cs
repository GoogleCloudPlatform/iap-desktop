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

    public abstract class SettingBase<T> : ISetting<T>
    {
        private T currentValue;

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------

        public string Key { get; }

        public string Title { get; }

        public string Description { get; }

        //---------------------------------------------------------------------
        // Value.
        //---------------------------------------------------------------------

        public bool IsDirty { get; private set; } = false;

        public T DefaultValue { get; }

        public bool IsDefault => Equals(this.DefaultValue, this.Value);

        public object Value
        {
            get => this.currentValue;
            set
            {
                this.currentValue = (T)value;
                this.IsDirty = true;
            }
        }

        //---------------------------------------------------------------------
        // Overlay.
        //---------------------------------------------------------------------

        public ISetting<T> OverlayBy(ISetting<T> overlaySetting)
        {
            return overlaySetting.IsDefault
                ? this  // Overlay if it is just supplying a default value
                : overlaySetting;   // Overlay has a relevant value.
        }

        public ISetting OverlayBy(ISetting setting)
            => OverlayBy((ISetting<T>)setting);

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SettingBase(
            string key,
            string title,
            string description,
            T initialValue,
            T defaultValue)
        {
            this.Key = key;
            this.Title = title;
            this.Description = description;
            this.currentValue = initialValue;
            this.DefaultValue = defaultValue;

            Debug.Assert(!this.IsDirty);
        }
    }
}
