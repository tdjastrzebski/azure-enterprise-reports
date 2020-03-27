/*
 * Copyright © Tomasz Jastrzębski 2019-2020
 */
using CsvHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

/// <remarks>
/// Too short database column length may result in mysterious SqlException exception:
/// "A transport-level error has occurred when receiving results from the server. - The specified network name is no longer available".
/// </remarks>
class CsvDataReader<T> : DbDataReader
{
    private readonly CsvReader _csvReader;
    private int _recordCount = 0;
    private readonly Func<T, bool> _sink;
    private T _currentRecord;
    public readonly Dictionary<string, int> MaxLenghts;
    private readonly bool _trackMaxLenghts;
    private readonly PropertyInfo[] _properties;

    public CsvDataReader(StreamReader streamReader, Func<T, bool> sink = null, bool trackMaxLenghts = false)
    {
        _csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        _csvReader.Configuration.Delimiter = CultureInfo.InvariantCulture.TextInfo.ListSeparator;
        _csvReader.Read();
        _csvReader.ReadHeader();

        _sink = sink;

        var map = _csvReader.Configuration.AutoMap<T>();
        _properties = map.MemberMaps.Select(m => (PropertyInfo)m.Data.Member).ToArray();

        _trackMaxLenghts = trackMaxLenghts;

        if (trackMaxLenghts) {
            MaxLenghts = new Dictionary<string, int>(FieldCount);

            for (int i = 0; i < FieldCount; i++) {
                MaxLenghts.Add(GetName(i), 0);
            }
        }
    }

    public override bool Read()
    {
        do {
            try {
                if (!_csvReader.Read()) return false;
            } catch (ParserException e) {
                throw new ApplicationException("Error while parsing the following line:\n" + e.ReadingContext.RawRecord, e);
            }
            _currentRecord = _csvReader.GetRecord<T>();

            if (_trackMaxLenghts && _currentRecord != null) {
                for (int i = 0; i < FieldCount; i++) {
                    string fieldName = GetName(i);
                    int fieldLength = MaxLenghts[fieldName];
                    object val = GetValue(i);

                    if (val != null) {
                        string s = val.ToString();
                        if (s.Length > fieldLength) MaxLenghts[fieldName] = s.Length;
                    }
                }
            }
        } while (_currentRecord == null || (_sink != null && !_sink(_currentRecord)));

        _recordCount++;
        return true;
    }

    public override int FieldCount {
        get { return _properties.Length; }
    }

    public override int RecordsAffected {
        get { return _recordCount; }
    }

    public override bool HasRows {
        get { return true; }
    }

    protected override void Dispose(bool disposing)
    {
        _csvReader.Dispose();
    }

    public override string GetName(int i)
    {
        return _properties[i].Name;
    }

    public override int GetOrdinal(string name)
    {
        var prop = _properties.Single(m => m.Name == name);
        return Array.IndexOf(_properties, prop);
    }

    public override object GetValue(int i)
    {
        PropertyInfo prop = _properties[i];
        return prop.GetValue(_currentRecord);
    }

    public override bool IsDBNull(int i)
    {
        object value = GetValue(i);
        return value == null;
    }

    public override bool NextResult()
    {
        return false; // single result only
    }

    public override Type GetFieldType(int i)
    {
        PropertyInfo prop = _properties[i];
        return prop.PropertyType;
    }

    #region not implemented

    public override object this[string name] {
        get { throw new NotImplementedException(); }
    }

    public override object this[int i] {
        get { throw new NotImplementedException(); }
    }

    public override int Depth {
        get { throw new NotImplementedException(); }
    }

    public override bool IsClosed {
        get { throw new NotImplementedException(); }
    }

    public override bool GetBoolean(int i)
    {
        throw new NotImplementedException();
    }

    public override byte GetByte(int i)
    {
        throw new NotImplementedException();
    }

    public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public override char GetChar(int i)
    {
        throw new NotImplementedException();
    }

    public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public override string GetDataTypeName(int i)
    {
        throw new NotImplementedException();
    }

    public override DateTime GetDateTime(int i)
    {
        throw new NotImplementedException();
    }

    public override decimal GetDecimal(int i)
    {
        throw new NotImplementedException();
    }

    public override double GetDouble(int i)
    {
        throw new NotImplementedException();
    }

    public override float GetFloat(int i)
    {
        throw new NotImplementedException();
    }

    public override Guid GetGuid(int i)
    {
        throw new NotImplementedException();
    }

    public override short GetInt16(int i)
    {
        throw new NotImplementedException();
    }

    public override int GetInt32(int i)
    {
        throw new NotImplementedException();
    }

    public override long GetInt64(int i)
    {
        throw new NotImplementedException();
    }

    public override string GetString(int i)
    {
        throw new NotImplementedException();
    }

    public override int GetValues(object[] values)
    {
        throw new NotImplementedException();
    }

    public override IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    #endregion
}
