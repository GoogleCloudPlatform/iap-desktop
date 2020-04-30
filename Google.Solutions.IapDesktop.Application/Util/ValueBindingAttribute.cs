using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Util
{
    /// <summary>
    /// Defines a data binding between a property and an external value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class ValueBindingAttribute : Attribute
    {
        public string Name { get; }
        public Type PropertyType { get; }

        protected ValueBindingAttribute(string name, Type propertyType)
        {
            Utilities.ThrowIfNullOrEmpty(name, nameof(name));

            this.Name = name;
            this.PropertyType = propertyType;
        }

        protected bool IsPropertyOfExpectedType(PropertyInfo property)
        {
            bool propIsNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;

            //
            // Check if the property can be used for the given value kind.
            //
            if (!propIsNullable && property.PropertyType == this.PropertyType)
            {
                // Property matches the expected value kind.
                return true;
            }
            else if (propIsNullable && Nullable.GetUnderlyingType(property.PropertyType) == this.PropertyType)
            {
                // Property is nullable, but the underlying type matches the expected value kind.
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool IsValueOfExpectedType(object value)
        {
            if (value == null)
            {
                return true;
            }

            bool valueIsNullable = Nullable.GetUnderlyingType(value.GetType()) != null;

            //
            // Check if the value can be used for the given value kind.
            //
            if (!valueIsNullable && value.GetType() == this.PropertyType)
            {
                // Value matches the expected value kind.
                return true;
            }
            else if (valueIsNullable && Nullable.GetUnderlyingType(value.GetType()) == this.PropertyType)
            {
                // Value is nullable, but the underlying type matches the expected value kind.
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual void SetValue(object obj, PropertyInfo property, object value)
        {
            //
            // Check if the property can be used for the given value kind.
            //
            if (!IsPropertyOfExpectedType(property))
            {
                throw new InvalidCastException(
                    $"Property {property.PropertyType.Name} cannot be bound to a value of type {this.PropertyType}");
            }

            //
            // Check if the value can be used for the given value kind.
            //
            if (!IsValueOfExpectedType(value))
            {
                throw new InvalidCastException(
                    $"Value cannot be bound to a value of type {this.PropertyType}");
            }

            if (value == null)
            {
                // Nothing to do.
                return;
            }

            bool propIsNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
            bool valueIsNullable = Nullable.GetUnderlyingType(value.GetType()) != null;

            //
            // Convert value to Nullable if necessary.
            //
            if (propIsNullable == valueIsNullable)
            {
                // Straight assignment should work.

            }
            else if (propIsNullable && !valueIsNullable)
            {
                // Value needs to be wrapped.
                value = Activator.CreateInstance(
                    typeof(Nullable<>).MakeGenericType(value.GetType()),
                    value);
            }
            else
            {
                throw new InvalidCastException(
                    $"Value of type {property.PropertyType.Name} cannot be assigned to {value.GetType().Name}");
            }

            if (!property.PropertyType.IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException(
                    $"Property {property.Name} must be of type {this.PropertyType.Name} (or a nullable thereof) to bind");
            }

            property.SetValue(obj, value);
        }

        public virtual object GetValue(object obj, PropertyInfo property)
        {
            //
            // Check if the property can be used for the given value kind.
            //
            if (!IsPropertyOfExpectedType(property))
            {
                throw new InvalidCastException(
                    $"Property {property.PropertyType.Name} cannot be bound to a value of type {this.PropertyType}");
            }

            return property.GetValue(obj);
        }

    }
}
