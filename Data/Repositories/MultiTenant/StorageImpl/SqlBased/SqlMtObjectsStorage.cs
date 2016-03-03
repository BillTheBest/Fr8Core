﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Data.Repositories.MultiTenant.Ast;

namespace Data.Repositories.MultiTenant.Sql
{
    public class SqlMtObjectsStorage : IMtObjectsStorage
    {
        private readonly string _connectionString;
        private readonly IMtObjectConverter _converter;

        public SqlMtObjectsStorage(IMtObjectConverter converter)
        {
            _converter = converter;
            _connectionString = ConfigurationManager.ConnectionStrings["DockyardDB"].ConnectionString;
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(_connectionString);

            connection.Open();
            return connection;
        }
        
        private int Upsert(string fr8AccountId, MtObject obj, AstNode @where, bool allowUpdate, bool allowInsert)
        {
            var fields = new List<string>
            {
                "Type",
                "CreatedAt",
                "UpdatedAt",
                "fr8AccountId",
                "IsDeleted"
            };

            var parameters = new List<string>
            {
                "@type",
                "@created",
                "@updated",
                "@account",
                "@isDeleted"
            };

            foreach (var mtPropertyInfo in obj.MtTypeDefinition.Properties)
            {
                parameters.Add("@val" + (mtPropertyInfo.Index + 1));
                fields.Add("Value" + (mtPropertyInfo.Index + 1));
            }

            var tableDefintion = string.Join(", ", fields);

            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.Parameters.AddWithValue("@type", obj.MtTypeDefinition.Id);
                    command.Parameters.AddWithValue("@created", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@updated", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@account", fr8AccountId);
                    command.Parameters.AddWithValue("@isDeleted", false);

                    foreach (var mtPropertyInfo in obj.MtTypeDefinition.Properties)
                    {
                        var value = obj.Values[mtPropertyInfo.Index];
                        object dbValue = DBNull.Value;

                        if (value != null)
                        {
                            dbValue = value;
                        }

                        command.Parameters.AddWithValue("@val" + (mtPropertyInfo.Index + 1), dbValue);
                    }

                    if (@where != null)
                    {
                        var valuesToInsert = string.Join(", ", fields.Select(x => "Src." + x));
                        var astConverter = new AstToSqlConverter(obj.MtTypeDefinition, _converter, "Tgt");

                        astConverter.Convert(@where);

                        var cmd = string.Format(@"merge MtData as Tgt 
                                               using (select {0}) as Src ({1}) 
                                               ON Tgt.Type = @type and Tgt.fr8AccountId = @account and ({2}) and Tgt.IsDeleted = 0", string.Join(",", parameters), tableDefintion, astConverter.SqlCommand);

                        if (allowUpdate)
                        {
                            cmd += string.Format("\nwhen matched then update set {0}", string.Join(", ", fields.Where(x => x != "CreatedAt").Select(x => string.Format("Tgt.{0} = Src.{0}", x))));
                        }

                        if (allowInsert)
                        {
                            cmd += string.Format("\nwhen not matched then insert ({0}) values ({1});", tableDefintion, valuesToInsert);
                        }

                        command.CommandText = cmd;

                        for (int index = 0; index < astConverter.Constants.Count; index++)
                        {
                            command.Parameters.AddWithValue("@param" + index, astConverter.Constants[index]);
                        }
                    }
                    else
                    {
                        if (!allowInsert)
                        {
                            return 0;
                        }

                        command.CommandText = string.Format(@"insert into MtData ({0}) values ({1})", tableDefintion, string.Join(",", parameters));
                    }

                    var affectedRows = command.ExecuteNonQuery();

                  

                    return affectedRows;
                }
            }
        }

        public int Upsert(string fr8AccountId, MtObject obj, AstNode @where)
        {
            return Upsert(fr8AccountId, obj, @where, true, true);
        }

        public int Insert(string fr8AccountId, MtObject obj, AstNode uniqueConstraint)
        {
            var affectedRows = Upsert(fr8AccountId, obj, uniqueConstraint, false, true);

            if (affectedRows == 0)
            {
                throw new Exception("Violation of unique constraint");
            }

            return affectedRows;
        }
        
        public int Update(string fr8AccountId, MtObject obj, AstNode @where)
        {
            return Upsert(fr8AccountId, obj, @where, true, false);
        }

        public IEnumerable<MtObject> Query(string fr8AccountId, MtTypeDefinition type, AstNode @where)
        {
            var fields = new List<string>
            {
                "Type",
                "fr8AccountId",
                "IsDeleted"
            };

            var parameters = new List<string>
            {
                "@type",
                "@account",
                "@isDeleted"
            };

            foreach (var mtPropertyInfo in type.Properties)
            {
                parameters.Add("@val" + (mtPropertyInfo.Index + 1));
                fields.Add("Value" + (mtPropertyInfo.Index + 1));
            }

            var tableDefintion = string.Join(", ", fields);

            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.Parameters.AddWithValue("@type", type.Id);
                    command.Parameters.AddWithValue("@account", fr8AccountId);
                    command.Parameters.AddWithValue("@isDeleted", false);

                    string cmd = string.Format("select {0} from MtData where Type=@type and fr8AccountId=@account and IsDeleted = @isDeleted", tableDefintion);

                    if (where != null)
                    {
                        var astConverter = new AstToSqlConverter(type, _converter);

                        astConverter.Convert(@where);

                        cmd += " and " + astConverter.SqlCommand;

                        for (int index = 0; index < astConverter.Constants.Count; index++)
                        {
                            command.Parameters.AddWithValue("@param" + index, astConverter.Constants[index]);
                        }
                    }

                    command.CommandText = cmd;
                    
                    var result = new List<MtObject>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var obj = new MtObject(type);

                            foreach (var mtPropertyInfo in type.Properties)
                            {
                                var val = reader["Value" + (mtPropertyInfo.Index+1)];

                                if (val != DBNull.Value)
                                {
                                    obj.Values[mtPropertyInfo.Index] = (string) val;
                                }
                            }

                            result.Add(obj);
                        }
                    }

                    return result;
                }
            }
        }

        public int Delete(string fr8AccountId, MtTypeDefinition type, AstNode @where)
        {
            throw new System.NotImplementedException();
        }
    }
}
