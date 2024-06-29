using System.Xml.Serialization;

namespace ECBCurrencyRates.ECBIntegration.Models
{
  public class ECBSeriesModel
  {
    // Define your ECBSeriesModel classes here based on the XML structure
    [XmlRoot(ElementName = "GenericData", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/message")]
    public class GenericData
    {
      [XmlElement(ElementName = "DataSet", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/message")]
      public DataSet DataSet { get; set; }
    }

    [XmlRoot(ElementName = "DataSet", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/message")]
    public class DataSet
    {
      [XmlElement(ElementName = "Series", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public List<Series> Series { get; set; }
    }

    [XmlRoot(ElementName = "Series", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
    public class Series
    {
      [XmlElement(ElementName = "SeriesKey", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public SeriesKey SeriesKey { get; set; }

      [XmlElement(ElementName = "Attributes", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public Attributes Attributes { get; set; }

      [XmlElement(ElementName = "Obs", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public Obs Obs { get; set; }
    }

    public class SeriesKey
    {
      [XmlElement(ElementName = "Value", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public List<SeriesValue> Values { get; set; }
    }

    public class SeriesValue
    {
      [XmlAttribute(AttributeName = "id")]
      public string Id { get; set; }

      [XmlAttribute(AttributeName = "value")]
      public string Value { get; set; }
    }

    public class Attributes
    {
      [XmlElement(ElementName = "Value", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public List<AttributesValue> Values { get; set; }
    }

    public class AttributesValue
    {
      [XmlAttribute(AttributeName = "id")]
      public string Id { get; set; }

      [XmlAttribute(AttributeName = "value")]
      public string Value { get; set; }
    }

    public class Obs
    {
      [XmlElement(ElementName = "ObsValue", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public ObsValue ObsValue { get; set; }

      [XmlElement(ElementName = "ObsDimension", Namespace = "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic")]
      public ObsValue ObsDimension { get; set; }
    }

    public class ObsValue
    {
      [XmlAttribute(AttributeName = "value")]
      public string Value { get; set; }
    }
  }
}
