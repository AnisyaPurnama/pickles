﻿#region License

/*
    Copyright [2011] [Jeffrey Cameron]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

#endregion

using System;
using System.Collections.Generic;

namespace PicklesDoc.Pickles.Parser
{
    public class Feature
    {
        public Feature()
        {
            this.FeatureElements = new List<IFeatureElement>();
            this.Tags = new List<string>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<IFeatureElement> FeatureElements { get; private set; }
        public Scenario Background { get; private set; }
        public List<string> Tags { get; set; }

        public void AddTag(string tag)
        {
            this.Tags.Add(tag);
        }

        public void AddBackground(Scenario background)
        {
            background.Feature = this;
            this.Background = background;
        }

        public void AddFeatureElement(IFeatureElement featureElement)
        {
            featureElement.Feature = this;
            this.FeatureElements.Add(featureElement);
        }
    }
}