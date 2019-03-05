// Sitecore.Marketing.Pipelines.ValidateJsonFieldLinks.ValidateProperties
namespace Sitecore.Support.Marketing.Pipelines.ValidateJsonFieldLinks
{
  using Microsoft.Extensions.Logging;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using Sitecore.Abstractions;
  using Sitecore.Data;
  using Sitecore.Framework.Conditions;
  using Sitecore.Links;
  using Sitecore.Marketing.Data.Fields;
  using Sitecore.Marketing.Pipelines.ValidateJsonFieldLinks;
  using Sitecore.Marketing.Pipelines.ValidateJsonPropertyLink;
  using System;
  using System.Collections.Generic;

  public class FixedValidateProperties : IValidateJsonFieldLinksProcessor
  {
    private const string ValidatePipelineName = "validateJsonPropertyLink";

    private readonly BaseCorePipelineManager _corePipeline;

    private readonly ILogger<ValidateProperties> _log;

    public FixedValidateProperties(ILogger<ValidateProperties> log, BaseCorePipelineManager corePipeline)
    {
      _log = log;
      _corePipeline = corePipeline;
    }

    public void Process(ValidateJsonFieldLinksArgs args)
    {
      Condition.Requires(args, "args").IsNotNull();
      if (args.SourceField != null)
      {
        string value = args.SourceField.Value;
        if (!string.IsNullOrEmpty(value))
        {
          var isArray = value.StartsWith("[");
          JObject jObject = null;
          JArray jArray = null;
          try
          {
            if (isArray)
            {
              jArray = JArray.Parse(value);

            }
            else
            {
              jObject = JObject.Parse(value);
            }
          }
          catch (JsonException exception)
          {

            ID iD = args.SourceField.InnerField.ID;
            _log.LogWarning(0, exception, "Failed to parse field value as JSON when validating JSON field {0} on item {1}", iD, args.SourceField.InnerField.Item.ID);
            return;
          }
          if (isArray)
          {
            ProcessArray(jArray, args.SourceField, args.Result);
          }
          else
          {
            ProcessObject(jObject, args.SourceField, args.Result);
          }
        }
      }
    }

    private void ProcessArray(JArray jArray, JsonField sourceField, LinksValidationResult result)
    {
      foreach (JObject item in jArray)
      {
        ProcessObject(item, sourceField, result);
      }
    }

    private void ProcessObject(JObject value, JsonField sourceField, LinksValidationResult result)
    {
      foreach (KeyValuePair<string, JToken> item in value)
      {
        ProcessValue(item.Key, item.Value, sourceField, result);
      }
    }

    private void ProcessValue(string key, JToken value, JsonField sourceField, LinksValidationResult result)
    {
      if (value is JObject)
      {
        ProcessObject((JObject)value, sourceField, result);
      }
      else if (value is JArray)
      {
        ProcessArrayProperty(key, (JArray)value, sourceField, result);
      }
      else
      {
        ProcessSimpleProperty(key, value.ToString(), sourceField, result);
      }
    }

    private void ProcessSimpleProperty(string key, string value, JsonField sourceField, LinksValidationResult result)
    {
      ValidateJsonPropertyLinkArgs args = new ValidateJsonPropertyLinkArgs(sourceField, key, value, result);
      _corePipeline.Run("validateJsonPropertyLink", args);
    }

    private void ProcessArrayProperty(string key, JArray array, JsonField sourceField, LinksValidationResult result)
    {
      foreach (JToken item in array)
      {
        ProcessValue(key, item, sourceField, result);
      }
    }
  }
}
