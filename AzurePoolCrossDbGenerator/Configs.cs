using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace AzurePoolCrossDbGenerator
{
    public class Configs
    {
        /// <summary>
        /// A base class for multiple configuration types.
        /// </summary>
        public abstract class GenericConfigEntry
        {
            public string folder;
            public string localDB;

            /// <summary>
            /// Add missing values from mergeFrom to this.
            /// </summary>
            /// <param name="mergeFrom"></param>
            public GenericConfigEntry Merge(GenericConfigEntry mergeFrom)
            {
                // get list of public fields
                Type myType = mergeFrom.GetType();
                FieldInfo[] myField = myType.GetFields();

                // copy values on those with missing values
                foreach (var field in myField)
                {
                    string name = field.Name;
                    string from = (string)myType.GetField(name).GetValue(mergeFrom);
                    string to = (string)myType.GetField(name).GetValue(this);
                    if (to == null) myType.GetField(name).SetValue(this, from);
                }

                return this;
            }
        }

        public class CreateMasterKey : GenericConfigEntry
        {
            public string password;
            public string credential;
            public string identity;
            public string secret;
        }

        public class CreateExternalDataSource : GenericConfigEntry
        {
            public string externalDB;
            public string serverName;
            public string sourceNamePrefix;
            public string credential;
            public string twoway;
        }



    }
}
