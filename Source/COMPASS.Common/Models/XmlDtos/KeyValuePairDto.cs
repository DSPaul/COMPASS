using System.Collections.Generic;

namespace COMPASS.Common.Models.XmlDtos;

public class KeyValuePairDto<TK, TV>
{
    //Paramterless ctor required for deserialization
    public KeyValuePairDto() { }
    
    public KeyValuePairDto(KeyValuePair<TK, TV> kvp)
    {
        Key = kvp.Key;
        Value = kvp.Value;
    }

    public TK? Key { get; set; }
    public TV? Value { get; set; }
}