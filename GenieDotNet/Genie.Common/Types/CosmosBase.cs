
namespace Genie.Common.Types;

public abstract record CosmosBase : CosmosIdentifier
{
    private DateTime? _created = null; // need 3 significant digits
    public DateTime? Created { 
        get { return _created; } 
        set { 
            if (value == null) _created = null; 
            else _created = new DateTime(value.Value.Ticks - (value.Value.Ticks % 10000), value.Value.Kind); 
        } 
    }
    public string _rid { get; set; } = "";
    public string _self { get; set; } = "";
    public string _etag { get; set; } = "";
    public string _attachments { get; set; } = "";
    public int? _ts { get; set; }
    public int? _ttl { get; set; }


    public bool IsNew() => string.IsNullOrEmpty(_rid);


}