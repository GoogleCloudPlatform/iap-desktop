using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Google.Solutions.IapDesktop.Application.Settings
{

    public abstract class SettingBase<T> : ISetting<T>
    {
        private T currentValue;

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------

        public string Key { get; }

        public string Title { get; }

        public string Description { get; }

        public string Category { get; }

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
                T typedValue;
                if (value is string stringValue)
                {
                    typedValue = Parse(stringValue);
                }
                else if (value is null)
                {
                    typedValue = Parse(null);
                }
                else if (value is T t)
                {
                    typedValue = t;
                }
                else
                {
                    throw new InvalidCastException(
                        "Value must be of type " + typeof(T).Name);
                }

                if (!IsValid(typedValue))
                {
                    throw new ArgumentOutOfRangeException(
                        $"Value {value} is not within the permitted range");
                }

                this.currentValue = typedValue;
                this.IsDirty = !Equals(value, this.DefaultValue);
            }
        }

        //---------------------------------------------------------------------
        // Overlay.
        //---------------------------------------------------------------------

        public ISetting<T> OverlayBy(ISetting<T> overlaySetting)
        {
            // NB. The idea of overlaying is that you use a base setting
            // (root), overlay it with a more specific setting, overlay
            // it with a more specific setting one again, etc... walking
            // your way from the root to the leaf.

            Debug.Assert(overlaySetting.Key == this.Key);
            Debug.Assert(overlaySetting.Title == this.Title);
            Debug.Assert(overlaySetting.Description == this.Description);
            Debug.Assert(overlaySetting.Category == this.Category);

            if (overlaySetting.IsDefault)
            {
                // Overlay does not add anything new, but own setting
                // becomes new default.
                return CreateNew(
                    (T)this.Value,
                    (T)this.Value);
            }
            else
            {
                // Overlay changes the effective setting, with
                // own setting serving as the new default.
                return CreateNew(
                    (T)overlaySetting.Value,
                    (T)this.Value);
            }
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
            string category,
            T initialValue,
            T defaultValue)
        {
            this.Key = key;
            this.Title = title;
            this.Description = description;
            this.Category = category;
            this.currentValue = initialValue;
            this.DefaultValue = defaultValue;
            
            Debug.Assert(!this.IsDirty);
        }

        protected abstract bool IsValid(T value);

        protected abstract T Parse(string value);

        protected abstract SettingBase<T> CreateNew(T value, T defaultValue);
    }
}
