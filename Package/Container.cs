﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Zenet.Package
{
    public class Container : IContainer
    {
        #region Header

        private int _index;
        private List<byte[]> _data;
        private List<string> _errors;
        private byte[] _target;

        public int Index => _index;

        public byte[] Serialized => _data.SelectMany(bytes => bytes).ToArray();

        public byte[] Deserialize { set => _target = value; }

        public string[] Errors => _errors.ToArray();

        #endregion

        #region Init

        public Container()
        {
            _index = 0;
            _data = new List<byte[]>();
            _errors = new List<string>();
            _target = null;
        }

        #endregion

        #region Add

        public void Add(byte value)
        {
            _data.Add(new byte[1] { value });
        }

        public void Add(int value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(short value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(long value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(float value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(double value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(string value)
        {
            Add(Encoding2.Bytes(value, Encode.UTF8));
        }

        public void Add(char value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(bool value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(byte[] value)
        {
            Add(value.Length);
            _data.Add(value);
        }

        public void Add(Vec3 value)
        {
            Add(Vec3.ToBytes(value));
        }

        public void Add(Vec2 value)
        {
            Add(Vec2.ToBytes(value));
        }

        #endregion

        #region Get

        public bool GetBool()
        {
            try
            {
                var value = BitConverter.ToBoolean(_target, _index);
                _index += sizeof(bool);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetBool)}] on index {_index}");
                return false;
            }
        }

        public byte GetByte()
        {
            try
            {
                byte value = _target[_index];
                _index += sizeof(byte);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetByte)}] on index {_index}");
                return 0;
            }
        }

        public byte[] GetBytes()
        {
            try
            {
                var length = GetInt();
                var value = new byte[length];
                Buffer.BlockCopy(_target, _index, value, 0, length);

                _index += value.Length;

                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetBytes)}] on index {_index}");
                return null;
            }
        }

        public char GetChar()
        {
            try
            {
                char value = BitConverter.ToChar(_target, _index);
                _index += sizeof(char);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetChar)}] on index {_index}");
                return new char();
            }
        }

        public double GetDouble()
        {
            try
            {
                double value = BitConverter.ToDouble(_target, _index);
                _index += sizeof(double);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetDouble)}] on index {_index}");
                return 0;
            }
        }

        public float GetFloat()
        {
            try
            {
                float value = BitConverter.ToSingle(_target, _index);
                _index += sizeof(float);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetFloat)}] on index {_index}");
                return 0;
            }
        }

        public int GetInt()
        {
            try
            {
                int value = BitConverter.ToInt32(_target, _index);
                _index += sizeof(int);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetInt)}] on index {_index}");
                return 0;
            }
        }

        public long GetLong()
        {
            try
            {
                var value = BitConverter.ToInt64(_target, _index);
                _index += sizeof(long);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetLong)}] on index {_index}");
                return 0;
            }
        }

        public short GetShort()
        {
            try
            {
                short value = BitConverter.ToInt16(_target, _index);
                _index += sizeof(short);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetShort)}] on index {_index}");
                return 0;
            }
        }

        public string GetString()
        {
            try
            {
                var data = GetBytes();
                var text = Encoding2.String(data, Encode.UTF8);

                if (string.IsNullOrEmpty(text))
                {
                    _errors.Add($"[{nameof(GetString)}] on index {_index}: {nameof(string.IsNullOrEmpty)}");
                    return null;
                }

                return text;
            }
            catch
            {
                _errors.Add($"[{nameof(GetString)}] on index {_index}");
                return null;
            }
        }

        public Vec2 GetVec2()
        {
            try
            {
                var data = GetBytes();
                var vec2 = Vec2.ToVec2(data);

                if (vec2 == null)
                {
                    _errors.Add($"[{nameof(GetVec2)}] on index {_index}: {"Vec2 not found"}");
                    return null;
                }

                return vec2;
            }
            catch
            {
                _errors.Add($"[{nameof(GetVec2)}] on index {_index}");
                return null;
            }
        }

        public Vec3 GetVec3()
        {
            try
            {
                var data = GetBytes();
                var vec3 = Vec3.ToVec3(data);

                if (vec3 == null)
                {
                    _errors.Add($"[{nameof(GetVec3)}] on index {_index}: {"Vec3 not found"}");
                    return null;
                }

                return vec3;
            }
            catch
            {
                _errors.Add($"[{nameof(GetVec3)}] on index {_index}");
                return null;
            }
        }

        #endregion
    }
}