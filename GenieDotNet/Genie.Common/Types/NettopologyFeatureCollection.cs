using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Collections.ObjectModel;

namespace Genie.Common.Types;

public record NetTopologyFeatureCollection
{
    public Collection<IFeature> Features { get; set; } = [];

    public static string Serialize(IFeature json)
    {
        return new GeoJsonWriter().Write(json);
    }

    public static NetTopologySuite.Features.Feature Deserialize(string s)
    {
        return new GeoJsonReader().Read<NetTopologySuite.Features.Feature>(s);
    }

    // Copied from NetTopologySuite.Features.FeatureCollection but currently not used
    #region 

    private Envelope? _boundingBox;

    public Envelope? BoundingBox
    {
        get
        {
            _boundingBox ??= ComputeBoundingBox();

            return _boundingBox?.Copy();
        }
        set
        {
            _boundingBox = value;
        }
    }

    private Envelope? ComputeBoundingBox()
    {
        if (!NetTopologySuite.Features.Feature.ComputeBoundingBoxWhenItIsMissing)
            return null;

        Envelope envelope = new();
        foreach (IFeature item in this.Features)
        {
            if (item != null)
            {
                if (item.BoundingBox != null)
                    envelope.ExpandToInclude(item.BoundingBox);
                else if (item.Geometry is not null)
                    envelope.ExpandToInclude(item.Geometry.EnvelopeInternal);
            }
        }

        return envelope;
    }
    #endregion
}