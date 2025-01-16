using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("Report")]
public class Report
{
    [XmlElement("TestScript")]
    public TestScript TestScript { get; set; }
}

public class TestScript
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("Reporter")]
    public Reporter Reporter { get; set; }
}

public class Reporter
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("status")]
    public string Status { get; set; }

    [XmlAttribute("version")]
    public string Version { get; set; }

    [XmlElement("StartTime")]
    // Add XmlDateTimeSerializerFormat attribute to specify exact format
    [XmlDateTimeSerializerFormat("MM/dd/yyyy HH:mm:ss tt")]
    public DateTime StartTime { get; set; }

    [XmlElement("EndTime")]
    [XmlDateTimeSerializerFormat("MM/dd/yyyy HH:mm:ss tt")]
    public DateTime EndTime { get; set; }

    [XmlElement("PassedCount")]
    public int PassedCount { get; set; }

    [XmlElement("WarningCount")]
    public int WarningCount { get; set; }

    [XmlElement("FailedCount")]
    public int FailedCount { get; set; }

    [XmlElement("Iterations")]
    public int Iterations { get; set; }

    [XmlElement("StepCount")]
    public int StepCount { get; set; }

    [XmlElement("TotalSteps")]
    public int TotalSteps { get; set; }

    [XmlElement("ReportItems")]
    public ReportItems ReportItems { get; set; }
}

public class ReportItems
{
    [XmlElement("ReportItem")]
    public List<ReportItem> Items { get; set; }
}

public class ReportItem
{
    [XmlAttribute("caption")]
    public string Caption { get; set; }

    [XmlAttribute("eventId")]
    public int EventId { get; set; }

    [XmlAttribute("hasScreenshot")]
    public bool HasScreenshot { get; set; }

    [XmlAttribute("id")]
    public string Id { get; set; }

    [XmlAttribute("result")]
    public string Result { get; set; }

    [XmlAttribute("stepNumber")]
    public int StepNumber { get; set; }

    [XmlElement("Iteration")]
    public int Iteration { get; set; }

    [XmlElement("ActualResult")]
    public string ActualResult { get; set; }

    [XmlElement("ExpectedResult")]
    public string ExpectedResult { get; set; }

    [XmlElement("PerformanceTime")]
    public string PerformanceTime { get; set; }

    [XmlElement("ConsoleOutput")]
    public string ConsoleOutput { get; set; }
}

// Create a custom attribute to handle datetime format
[AttributeUsage(AttributeTargets.Property)]
public class XmlDateTimeSerializerFormatAttribute : Attribute
{
    public string Format { get; }

    public XmlDateTimeSerializerFormatAttribute(string format)
    {
        Format = format;
    }
}