// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Linq;

    public class HealthIndexCalculator
    {
        private CalculationMode calculationMode;

        public HealthIndexCalculator(ServiceContext serviceParamaters)
        {
            if (serviceParamaters.CodePackageActivationContext.GetConfigurationPackageNames().Contains("Config"))
            {
                ConfigurationPackage configPackage = serviceParamaters.CodePackageActivationContext.GetConfigurationPackageObject("Config");

                this.UpdateConfigSettings(configPackage.Settings);

                serviceParamaters.CodePackageActivationContext.ConfigurationPackageModifiedEvent
                    += this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;
            }
            else
            {
                this.calculationMode = CalculationMode.Simple;
            }
        }

        public HealthIndex ComputeIndex(int value)
        {
            return new HealthIndex(value, (this.calculationMode == CalculationMode.Simple) ? false : true);
        }

        public HealthIndex ComputeIndex(HealthIndex value)
        {
            return new HealthIndex(value.GetValue(), (this.calculationMode == CalculationMode.Simple) ? false : true);
        }

        public HealthIndex ComputeAverageIndex(IEnumerable<HealthIndex> indices)
        {
            return this.ComputeIndex((int)Math.Round(indices.Average(x => x.GetValue()), 0));            
        }

        private void UpdateConfigSettings(ConfigurationSettings configSettings)
        {
            try
            {
                KeyedCollection<string, ConfigurationProperty> parameters = configSettings.Sections["HealthIndexCalculator.Settings"].Parameters;

                string scoreCalculationMode = parameters["ScoreCalculationMode"].Value;

                this.calculationMode = String.Equals("Mode1", scoreCalculationMode, StringComparison.OrdinalIgnoreCase)
                    ? CalculationMode.Simple
                    : CalculationMode.Detailed;
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.UpdateConfigSettings(e.NewPackage.Settings);
        }
    }
}