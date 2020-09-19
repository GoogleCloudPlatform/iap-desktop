using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Settings
{

    public abstract class SettingBase<T> : ISetting<T>
    {
        private T currentValue;
        private Func<T, bool> validate;

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
                if (value is T typedValue)
                {
                    if (validate != null && !this.validate(typedValue))
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Value {value} is not within the permitted range");
                    }

                    this.currentValue = typedValue;
                    this.IsDirty = true;
                }
                else
                {
                    throw new InvalidCastException(
                        "Value must be of type " + typeof(T).Name);
                }
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
            T defaultValue,
            Func<T, bool> validate)
        {
            this.Key = key;
            this.Title = title;
            this.Description = description;
            this.currentValue = initialValue;
            this.DefaultValue = defaultValue;
            this.validate = validate;
            
            if (validate != null)
            {
                Debug.Assert(validate(initialValue));
                Debug.Assert(validate(defaultValue));
            }
            Debug.Assert(!this.IsDirty);
        }
    }
}
