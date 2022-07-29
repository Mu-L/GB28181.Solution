//-----------------------------------------------------------------------------
// Filename: SIPUserField.cs
//
// Description: 
// Encapsulates the format for the SIP Contact, From and To headers
//
// History:
// 21 Apr 2006	Aaron Clauson	Created.
// 04 Sep 2008  Aaron Clauson   Changed display name to always use quotes. Some SIP stacks were
//                              found to have porblems with a comma in a non-quoted display name.
// 06 Sep 2020	Edward Chen Refactoring
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Runtime.Serialization;
using GB28181.Logger4Net;
using SIPSorcery.SIP;
using SIPSorcery.Sys;

namespace GB28181
{
    /// <summary>
    /// name-addr      =  [ display-name ] LAQUOT addr-spec RAQUOT
    /// addr-spec      =  SIP-URI / SIPS-URI / absoluteURI
    /// SIP-URI          =  "sip:" [ userinfo ] hostport
    /// uri-parameters [ headers ]
    /// SIPS-URI         =  "sips:" [ userinfo ] hostport
    /// uri-parameters [ headers ]
    /// userinfo         =  ( user / telephone-subscriber ) [ ":" password ] "@"
    ///
    /// If no "<" and ">" are present, all parameters after the URI are header
    /// parameters, not URI parameters.
    /// </summary>
    [DataContract]
    public class SIPUserField : SIPSorcery.SIP.SIPUserField
    {
        private const char PARAM_TAG_DELIMITER = ';';

        private static ILog logger = AssemblyState.logger;


        [DataMember]
        public SIPParameters Parameters = new SIPParameters(null, PARAM_TAG_DELIMITER);

        public SIPUserField()
        { }

        public SIPUserField(string name, SIPURI uri, string paramsAndHeaders)
        {
            Name = name;
            URI = uri;

            Parameters = new SIPParameters(paramsAndHeaders, PARAM_TAG_DELIMITER);
        }

        public static SIPUserField ParseSIPUserField(string userFieldStr)
        {
            if (userFieldStr.IsNullOrBlank())
            {
                throw new ArgumentException("A SIPUserField cannot be parsed from an empty string.");
            }

            SIPUserField userField = new SIPUserField();
            string trimUserField = userFieldStr.Trim();

            int position = trimUserField.IndexOf('<');

            if (position == -1)
            {
                // Treat the field as a URI only, except that all parameters are Header parameters and not URI parameters 
                // (RFC3261 section 20.39 which refers to 20.10 for parsing rules).
                string uriStr = trimUserField;
                int paramDelimPosn = trimUserField.IndexOf(PARAM_TAG_DELIMITER);

                if (paramDelimPosn != -1)
                {
                    string paramStr = trimUserField.Substring(paramDelimPosn + 1).Trim();
                    userField.Parameters = new SIPParameters(paramStr, PARAM_TAG_DELIMITER);
                    uriStr = trimUserField.Substring(0, paramDelimPosn);
                }

                userField.URI = SIPURI.ParseSIPURI(uriStr);
            }
            else
            {
                if (position > 0)
                {
                    userField.Name = trimUserField.Substring(0, position).Trim().Trim('"');
                    trimUserField = trimUserField.Substring(position, trimUserField.Length - position);
                }

                int addrSpecLen = trimUserField.Length;
                position = trimUserField.IndexOf('>');
                if (position != -1)
                {
                    addrSpecLen = trimUserField.Length - 1;
                    if (position != -1)
                    {
                        addrSpecLen = position - 1;

                        string paramStr = trimUserField.Substring(position + 1).Trim();
                        userField.Parameters = new SIPParameters(paramStr, PARAM_TAG_DELIMITER);
                    }

                    string addrSpec = trimUserField.Substring(1, addrSpecLen);

                    userField.URI = SIPURI.ParseSIPURI(addrSpec);
                }
                else
                {
                    throw new SIPValidationException(SIPValidationFieldsEnum.ContactHeader, "A SIPUserField was missing the right quote, " + userFieldStr + ".");
                }
            }

            return userField;
        }



        public SIPUserField CopyOf()
        {
            SIPUserField copy = new SIPUserField
            {
                Name = Name,
                URI = URI.CopyOf(),
                Parameters = Parameters.CopyOf()
            };

            return copy;
        }
    }
}
