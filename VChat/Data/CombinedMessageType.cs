using System;

namespace VChat.Data
{
    public struct CombinedMessageType
        : IComparable<int>, IEquatable<int>,
          IComparable<Talker.Type>, IEquatable<Talker.Type>,
          IComparable<CustomMessageType>, IEquatable<CustomMessageType>,
          IComparable<CombinedMessageType>, IEquatable<CombinedMessageType>
    {
        public int Value { get; private set; }

        public Talker.Type? DefaultTypeValue
        {
            get => IsDefaultType() ? (Talker.Type)Value : null;
            private set => Value = (int)value.Value;
        }

        public CustomMessageType? CustomTypeValue
        {
            get => IsCustomType() ? (CustomMessageType)Value : null;
            private set => Value = (int)value.Value;
        }

        public CombinedMessageType(int value)
        {
            Value = value;
        }

        public CombinedMessageType(Talker.Type talkerType)
            : this((int)talkerType)
        {
        }

        public CombinedMessageType(CustomMessageType messageType)
            : this((int)messageType)
        {
        }

        public bool IsDefaultType()
        {
            return Enum.IsDefined(typeof(Talker.Type), Value);
        }

        public bool IsCustomType()
        {
            return Enum.IsDefined(typeof(CustomMessageType), Value);
        }

        public void Set(Talker.Type value)
        {
            DefaultTypeValue = value;
        }
        public void Set(CustomMessageType value)
        {
            CustomTypeValue = value;
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }

        public override string ToString()
        {
            if(IsDefaultType())
            {
                switch(DefaultTypeValue.Value)
                {
                    case Talker.Type.Normal:
                        return "Local";
                    case Talker.Type.Shout:
                        return "Shout";
                    case Talker.Type.Whisper:
                        return "Whisper";
                    case Talker.Type.Ping:
                        return "Ping";
                }
            }
            else if (IsCustomType())
            {
                switch (CustomTypeValue.Value)
                {
                    case CustomMessageType:
                        return "Global";
                }
            }

            return Value.ToString();
        }

        // Equal implementations
        public bool Equals(int other)
        {
            return other.Equals(Value);
        }
        public bool Equals(Talker.Type other)
        {
            return other.Equals(DefaultTypeValue);
        }
        public bool Equals(CustomMessageType other)
        {
            return other.Equals(CustomTypeValue);
        }
        public bool Equals(CombinedMessageType other)
        {
            return other.Value.Equals(Value);
        }
        
        // CompareTo implementations
        public int CompareTo(int other)
        {
            return other.CompareTo(Value);
        }
        public int CompareTo(Talker.Type other)
        {
            return other.CompareTo(DefaultTypeValue);
        }
        public int CompareTo(CustomMessageType other)
        {
            return other.CompareTo(CustomTypeValue);
        }
        public int CompareTo(CombinedMessageType other)
        {
            return other.Value.CompareTo(Value);
        }
    }
}
