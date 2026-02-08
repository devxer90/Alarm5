using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ALARm.Core;
using Dapper;
using Npgsql;

namespace ALARm.DataAccess
{
    public class ExportImportRepository : IExportImportRepository
    {
        public string ExportQueryReturnString(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<string>(text, commandType: CommandType.Text).FirstOrDefault();
            }
        }

        public bool Execute(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                try
                {
                    db.Execute(text, commandType: CommandType.Text);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public long ImportQueryReturnLong(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<long>(text, commandType: CommandType.Text).FirstOrDefault();
            }
        }

        public List<long> ImportQueryReturnListLong(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<long>(text, commandType: CommandType.Text).ToList();
            }
        }
        public List<string> ImportQueryReturnListString(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<string>(text, commandType: CommandType.Text).ToList();
            }
        }

        string IExportImportRepository.ExportQueryReturnString(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<string>(text, commandType: CommandType.Text).FirstOrDefault();
            }
        }

        bool IExportImportRepository.Execute(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                try
                {
                    db.Execute(text, commandType: CommandType.Text);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        long IExportImportRepository.ImportQueryReturnLong(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<long>(text, commandType: CommandType.Text).FirstOrDefault();
            }
        }

        double IExportImportRepository.ImportQueryReturnDouble(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<double>(text, commandType: CommandType.Text).FirstOrDefault();
            }
        }

        List<long> IExportImportRepository.ImportQueryReturnListLong(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<long>(text, commandType: CommandType.Text).ToList();
            }
        }

        List<string> IExportImportRepository.ImportQueryReturnListString(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<string>(text, commandType: CommandType.Text).ToList();
            }
        }
        List<ExportListPeriod> IExportImportRepository.ExportListPeriod(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ExportListPeriod>(text, commandTimeout: 7000, commandType: CommandType.Text).ToList();
            }
        }
        List<ExportList> IExportImportRepository.ExportList(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ExportList>(text, commandTimeout: 7000, commandType: CommandType.Text).ToList();
            }
        }

        List<ExportListPodgr> IExportImportRepository.ExportListPodgr(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ExportListPodgr>(text, commandTimeout: 7000, commandType: CommandType.Text).ToList();
            }
        }
        List<ImportListCurveID> IExportImportRepository.ImportQueryReturnListImportedCurve(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ImportListCurveID>(text, commandTimeout: 7000, commandType: CommandType.Text).ToList();
            }
        }
        List<ImportListEkasui> IExportImportRepository.ImportQueryReturnListImportedCurvedelete(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ImportListEkasui>(text, commandTimeout: 7000, commandType: CommandType.Text).ToList();
            }
        }

        
        List<ImportListElevationsix> IExportImportRepository.ImportQueryReturnListImportedElevation(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ImportListElevationsix>(text, commandTimeout: 7000, commandType: CommandType.Text).ToList();
            }
        }


        List<ImportListDirTrackID> IExportImportRepository.ImportQueryReturnListDirTrack(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ImportListDirTrackID>(text, commandType: CommandType.Text).ToList();
            }
        }
        List<ImportListSTRTrackID> IExportImportRepository.ImportQueryReturnListSTRTrack(string text)
        {
            using (IDbConnection db = new NpgsqlConnection(Helper.ConnectionString()))
            {
                if (db.State == ConnectionState.Closed)
                    db.Open();

                return db.Query<ImportListSTRTrackID>(text, commandType: CommandType.Text).ToList();
            }
        }



    }
}
