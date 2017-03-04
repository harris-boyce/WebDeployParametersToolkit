﻿using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace WebDeployParametersToolkit.Utilities
{
    public class WebConfigSettingsReader
    {
        public WebConfigSettingsReader(string fileName)
        {
            FileName = fileName;
        }

        public bool IncludeApplicationSettings { get; set; } = true;

        public bool IncludeAppSettings { get; set; } = true;

        public bool IncludeCompilationDebug { get; set; } = true;

        public bool IncludeMailSettings { get; set; } = true;

        public bool IncludeSessionStateSettings { get; set; } = true;

        public string FileName { get; }

        public IEnumerable<WebConfigSetting> Read()
        {
            var results = new List<WebConfigSetting>();

            if (File.Exists(FileName))
            {
                var document = new XmlDocument();
                document.Load(FileName);

                results.AddRange(ReadApplicationSettings(document, IncludeAppSettings, IncludeApplicationSettings));
                if (IncludeCompilationDebug)
                {
                    results.Add(new WebConfigSetting() { Name = "CompilationDebug", NodePath = "/configuration/system.web/compilation/@debug" });
                }
                if (IncludeMailSettings)
                {
                    results.Add(new WebConfigSetting() { Name = "Smtp.NeworkHost", NodePath = "/configuration/system.net/mailSettings/smtp/network/@host" });
                    results.Add(new WebConfigSetting() { Name = "Smtp.DeliveryMethod", NodePath = "/configuration/system.net/mailSettings/smtp/@deliveryMethod" });
                }
                if (IncludeSessionStateSettings)
                {
                    results.Add(new WebConfigSetting() { Name = "SessionState.Mode", NodePath = "/configuration/system.web/sessionState/@mode" });
                    results.Add(new WebConfigSetting() { Name = "SessionState.ConnectionString", NodePath = "/configuration/system.web/sessionState/sqlConnectionString" });
                }

            }
            return results;
        }

        public static IEnumerable<WebConfigSetting> ReadApplicationSettings(XmlDocument document, bool includeAppSettings, bool includeApplicationSettings)
        {
            var results = new List<WebConfigSetting>();

            if (includeAppSettings)
            {
                var appSettingsPath = "/configuration/appSettings/add";
                var appSettingsNodes = document.SelectNodes(appSettingsPath);
                for (int i = 0; i < appSettingsNodes.Count; i++)
                {
                    var node = appSettingsNodes[i];
                    var keyAttribute = node.Attributes["key"];
                    //var valueAttribute = node.Attributes["value"];
                    if (keyAttribute != null)
                    {
                        var settingName = keyAttribute.Value;
                        var settingPath = $"{appSettingsPath}[@key='{settingName}']/@value";
                        results.Add(new WebConfigSetting()
                        {
                            Name = settingName,
                            NodePath = settingPath
                        });
                    }
                }
            }

            if (includeApplicationSettings)
            {
                var basePath = "/configuration/applicationSettings";
                var settingsNode = document.SelectSingleNode(basePath);

                if (settingsNode != null)
                {
                    var nav = settingsNode.CreateNavigator();
                    if (nav.MoveToFirstChild())
                    {
                        do
                        {
                            var groupName = nav.Name;
                            var groupPath = $"{basePath}/{nav.Name}";
                            if (nav.MoveToFirstChild())
                            {
                                do
                                {
                                    var serializeAs = nav.GetAttribute("serializeAs", string.Empty);
                                    if (serializeAs == "String")
                                    {
                                        var settingName = nav.GetAttribute("name", string.Empty);
                                        var settingPath = $"{groupPath}/{nav.Name}[@name='{settingName}']/value/text()";

                                        if (results.Exists(s => s.Name == settingName))
                                        {
                                            settingName = $"{groupName}.{settingName}";
                                        }

                                        var setting = new WebConfigSetting()
                                        {
                                            NodePath = settingPath,
                                            Name = settingName
                                        };
                                        results.Add(setting);
                                    }
                                } while (nav.MoveToNext());
                            }
                            nav.MoveToParent();
                        } while (nav.MoveToNext());
                    }
                }
            }
            return results;
        }
    }
}