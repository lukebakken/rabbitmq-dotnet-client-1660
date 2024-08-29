using Genie.Common.Utils;
using Genie.Grpc;

namespace Genie.Common.Web
{
    public class MockPartyCreator
    {
        public static PartyBenchmarkRequest GetParty()
        {
            var p = new PartyBenchmarkRequest
            {
                Request = new BaseRequest
                {
                    CosmosBase = new CosmosBase
                    {
                        Identifier = new CosmosIdentifier
                        {
                            Id = Guid.NewGuid().ToString("N")
                        }
                    },
                    Origin = new Coordinate { Latitude = 38.897678, Longitude = -77.036552, Altitude = 0 }
                },
                Party = new Party
                {
                    CosmosBase = new CosmosBase
                    {
                        Identifier = new CosmosIdentifier
                        {
                            Id = Guid.NewGuid().ToString("N")
                        }
                    },
                    Name = "All The President's Men",
                    Type = Party.Types.PartyType.Party
                }
            };

            p.Party.Communications.Add(new PartyCommunication
            {
                BeginDate = Epoch.Convert(DateTime.UtcNow),
                CommunicationIdentity = new CommunicationIdentity
                {
                    GeographicLocation = new GeographicLocation
                    {
                        LocationName = "Pentagon",
                        LocationAddress = new LocationAddress { Line1Address = "1400 Defense Pentagon", MunicipalityName = "Washington", StateCode = "DC", PostalCode = "20301" },
                        GeoJsonLocation = new GeoJsonLocation
                        {
                            Circle = new GeoJsonCircle
                            {
                                Centroid = new Coordinate { Latitude = 38.870945, Longitude = -77.055252, Altitude = 0 },
                                Radius = 50
                            }
                        }
                    }
                }
            });

            //p.Party.Communications.Add(new PartyCommunication
            //{
            //    BeginDate = Epoch.Convert(DateTime.UtcNow),
            //    CommunicationIdentity = new CommunicationIdentity
            //    {
            //        GeographicLocation = new GeographicLocation
            //        {
            //            LocationName = "White House",
            //            LocationAddress = new LocationAddress { Line1Address = "1600 Pennsylvania Ave NW", MunicipalityName = "Washington", StateCode = "DC", PostalCode = "20500" },
            //            GeoJsonLocation = new GeoJsonLocation
            //            {
            //                Circle = new GeoJsonCircle
            //                {
            //                    Centroid = new Coordinate { Latitude = 38.897678, Longitude = -77.036552, Altitude = 0 },
            //                    Radius = 50
            //                }
            //            }
            //        }
            //    }
            //});

            return p;
        }
    }
}
