using DatabaseAccessor.DatabaseAccessor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using VPSCLS.Common;
using VPSCLS.Logging;

namespace VPSCLS.DBAccessor
{
    public class SQLWipReport : DBAccessor
    {
        private DBQue db = new DBQue();
        private const string WEB_TYPE = "Web";

        public DataTable GetAllWipInfo(string userId, string chassis, string shift, string engineNo, DateTime datetimeAsOf, List<string> selectedLots,
            List<string> selectedMatlGrps, List<string> selectedMatlGrpDescs, List<string> selectedColors, List<string> selectedOrders, List<string> selectedStations,
            List<string> selectedOperationActions, List<string> selectedLines, List<string> selectedReworkStations, string aging)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                string prod = ConfigurationManager.AppSettings["Production"].ToString();

                List<TimeSpan> ShiftTime = GetShiftTime(shift);
                List<String> AgingRange = GetAgingRange(aging);

                sql = @"
                SELECT                     
                    allData.VIN_NO, allData.CHASSIS_NO, allData.[ORDER], allData.SEQ_IN_LOT, allData.MATL_GROUP_DESC,
                    allData.MODEL_ID, allData.MATL_GROUP, allData.ENGINE_NO, allData.WBS_ELEM, allData.COLOR_CODE, allData.DESTINATION,
                    allData.AG_INTERIOR_COLOR, allData.AG_EXTERIOR_COLOR,
                    allData.STATION_ID, allData.STATION_NAME,
                    allData.OPERATION_ACTION, allData.F2_CASCADE, allData.REWORK, allData.UPDATE_DATETIME, allData.SAP_DATETIME, 
allData.AGING,
                    CASE WHEN(AG.CHASSIS_NO IS NOT NULL) THEN 'Y' ELSE 'N' END AS BLOCK_FLAG,
                    allData.LINEOFF_DATETIME
, allData.MATCHED_DATETIME
                FROM
                (
                    SELECT VM.VIN_NO, VM.CHASSIS_NO, VM.[ORDER], VM.SEQ_IN_LOT, VM.MATL_GROUP_DESC,
                    MM.MODEL_ID, MM.MATL_GROUP, VM.ENGINE_NO, VM.WBS_ELEM, VM.COLOR_CODE, VM.DESTINATION,
                    (SELECT AG_INTERIOR_COLOR FROM QRINFO WHERE VM.VIN_NO = VIN_NO) AS AG_INTERIOR_COLOR, VM.COLOR_DESC AS AG_EXTERIOR_COLOR,
                    (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE STATION_ID = VM.OPERATION_ID) AS STATION_ID,
                    (SELECT DISTINCT STATION_NAME FROM STATIONMST WHERE STATION_ID = VM.OPERATION_ID) AS STATION_NAME,
                    VM.OPERATION_ACTION,
                    CASE WHEN (GETDATE() > VM.CASCADE_DATETIME AND DATEDIFF(MINUTE, VM.CASCADE_DATETIME, GETDATE()) > 5) THEN 'Y' ELSE 'N' END AS 'F2_CASCADE',
                    (
	                    SELECT DISTINCT SM.STATION_ID
	                    FROM (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST) SM
	                    WHERE SM.STATION_ID = VM.TAKEOUT_STATION
	                    AND
	                    (
		                    SELECT TOP 1 rt1.ACTION	FROM REWORK_TIME rt1
		                    WHERE rt1.VIN_NO = VM.VIN_NO ORDER BY rt1.DATE_TIME DESC
	                    ) <> 'TakeIn'
	                    AND VM.TAKEOUT_STATION IS NOT NULL
	                    AND SM.STATION_NAME IS NOT NULL
                    ) AS REWORK,
                    VM.UPDATE_DATETIME,
                    VM.SAP_DATETIME,
VM.MATCHED_DATETIME,
                    DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) AS AGING,
                    (SELECT UPDATE_DATETIME FROM VEHICLE_HISTORY WHERE VIN_NO = VM.VIN_NO AND STATION_ID = (SELECT DISTINCT STATION_ID FROM STATIONMST	WHERE LINE_DESC = 'LINEOFF')) AS LINEOFF_DATETIME
                    FROM
                    (
	                    SELECT VM.*, SAH.SAP_DATETIME FROM VEHICLEMST VM
	                    LEFT JOIN
	                    (
		                    SELECT CHASSIS_NO, OPERATION_ACTION, STATION_ID, VIN_NO, MAX(DATETIME) AS SAP_DATETIME FROM SAPACTIONHISTORY
		                    GROUP BY CHASSIS_NO, OPERATION_ACTION, STATION_ID, VIN_NO
	                    ) SAH
	                    ON VM.OPERATION_ACTION = SAH.OPERATION_ACTION
	                    AND VM.CHASSIS_NO = SAH.CHASSIS_NO
                        WHERE 1=1";

                if (!string.IsNullOrEmpty(chassis))
                {
                    SqlParams["@chassis"] = chassis.ToUpper();
                    sql += @" AND VM.CHASSIS_NO LIKE '%' + @chassis + '%'";
                }

                if (!string.IsNullOrEmpty(engineNo))
                {
                    SqlParams["@engineNo"] = engineNo.ToUpper();
                    sql += @" AND UPPER(ENGINE_NO) LIKE '%' + @engineNo + '%'";
                }

                if (datetimeAsOf != DateTime.MinValue)
                {
                    SqlParams["@datetimeAsOf"] = datetimeAsOf;
                    sql += @" AND VM.UPDATE_DATETIME <= @datetimeAsOf";
                }

                if (AgingRange != null && AgingRange.Count == 2)
                {
                    String minAge = AgingRange[0];
                    String maxAge = AgingRange[1];
                    SqlParams["@MIN_AGE"] = minAge;
                    SqlParams["@MAX_AGE"] = maxAge;

                    if (!string.IsNullOrEmpty(minAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) >= @MIN_AGE";
                    } 
                    if (!string.IsNullOrEmpty(maxAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) <= @MAX_AGE";
                    } 

                }

                sql += @"
                    )
                    VM, MODELMST MM
                    WHERE VM.VIN_NO = VM.VIN_NO
                    AND VM.MODEL_NO = MM.MATL_GROUP
                    AND CASE WHEN (VM.OPERATION_ACTION = 'A080') THEN 'A070' ELSE VM.OPERATION_ACTION END NOT IN (SELECT DISTINCT OPERATION_ACTION FROM STATIONMST WHERE LINE_DESC = 'END')
                    AND VM.CHASSIS_NO NOT IN
                    (
	                    SELECT DISTINCT VIN FROM SAP_UPDATE
	                    WHERE CASE WHEN (ACTION_CODE = 'A080') THEN 'A070' ELSE ACTION_CODE END IN
	                    (SELECT DISTINCT OPERATION_ACTION FROM STATIONMST WHERE LINE_DESC = 'END')
                    )
                ) allData
                LEFT JOIN MODELMST MM
                ON MM.MATL_GROUP = allData.MATL_GROUP
                LEFT JOIN (SELECT DISTINCT CHASSIS_NO FROM AG_BLOCK) AG
                ON allData.CHASSIS_NO = AG.CHASSIS_NO
                WHERE 1=1 ";

                if (!string.IsNullOrEmpty(chassis))
                {
                    SqlParams["@chassis"] = chassis.ToUpper();
                    sql += @" AND allData.CHASSIS_NO LIKE '%' + @chassis + '%'";
                }

                if (!string.IsNullOrEmpty(engineNo))
                {
                    SqlParams["@engineNo"] = engineNo.ToUpper();
                    sql += @" AND UPPER(allData.ENGINE_NO) LIKE '%' + @engineNo + '%'";
                }

                if (datetimeAsOf != DateTime.MinValue)
                {
                    SqlParams["@datetimeAsOf"] = datetimeAsOf;
                    sql += @" AND allData.UPDATE_DATETIME < @datetimeAsOf";
                }

                if (selectedLots.Count() > 0)
                {
                    sql += " AND allData.WBS_ELEM IN (";
                    for (int i = 0; i < selectedLots.Count(); i++)
                    {
                        SqlParams["@lot" + i] = selectedLots.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != selectedLots.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrpDescs.Count() > 0)
                {
                    sql += " AND allData.MODEL_ID IN (";
                    for (int i = 0; i < selectedMatlGrpDescs.Count(); i++)
                    {
                        SqlParams["@matlGrpDesc" + i] = selectedMatlGrpDescs.ElementAt(i);

                        sql += "@matlGrpDesc" + i;
                        if (i != selectedMatlGrpDescs.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrps.Count() > 0)
                {
                    sql += " AND allData.MATL_GROUP IN (";
                    for (int i = 0; i < selectedMatlGrps.Count(); i++)
                    {
                        SqlParams["@matlGrp" + i] = selectedMatlGrps.ElementAt(i);

                        sql += "@matlGrp" + i;
                        if (i != selectedMatlGrps.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedColors.Count() > 0)
                {
                    sql += " AND allData.COLOR_CODE IN (";
                    for (int i = 0; i < selectedColors.Count(); i++)
                    {
                        SqlParams["@color" + i] = selectedColors.ElementAt(i);

                        sql += "@color" + i;
                        if (i != selectedColors.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedStations.Count() > 0)
                {
                    sql += " AND allData.STATION_ID IN (";
                    for (int i = 0; i < selectedStations.Count(); i++)
                    {
                        SqlParams["@station" + i] = selectedStations.ElementAt(i);

                        sql += "@station" + i;
                        if (i != selectedStations.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedLines.Count() > 0)
                {
                    sql += " AND MM.LINE IN (";
                    for (int i = 0; i < selectedLines.Count(); i++)
                    {
                        SqlParams["@line" + i] = selectedLines.ElementAt(i);

                        sql += "@line" + i;
                        if (i != selectedLines.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedOperationActions.Count() > 0)
                {
                    sql += " AND allData.OPERATION_ACTION IN (";
                    for (int i = 0; i < selectedOperationActions.Count(); i++)
                    {
                        SqlParams["@operationAction" + i] = selectedOperationActions.ElementAt(i);

                        sql += "@operationAction" + i;
                        if (i != selectedOperationActions.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedReworkStations.Count() > 0)
                {
                    sql += " AND allData.REWORK IN (";
                    for (int i = 0; i < selectedReworkStations.Count(); i++)
                    {
                        SqlParams["@rework" + i] = selectedReworkStations.ElementAt(i);

                        sql += "@rework" + i;
                        if (i != selectedReworkStations.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (ShiftTime != null && ShiftTime.Count == 2)
                {
                    TimeSpan startTime = ShiftTime[0];
                    TimeSpan endTime = ShiftTime[1];
                    SqlParams["@START_TIME"] = startTime;
                    SqlParams["@END_TIME"] = endTime;

                    if (startTime < endTime)
                    {
                        sql += @"
                        AND CONVERT(time(7), allData.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7), allData.UPDATE_DATETIME) < @END_TIME";
                    }
                    else
                    {
                        sql += @"
                        AND
                        (
	                        (CONVERT(time(7), allData.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7), allData.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7), allData.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7), allData.UPDATE_DATETIME) < @END_TIME)
                        )";
                    }
                }

                sql += @" ORDER BY allData.OPERATION_ACTION DESC, allData.SAP_DATETIME DESC, allData.UPDATE_DATETIME DESC";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllWipCount(string userId, string chassis, string shift, string engineNo, DateTime datetimeAsOf, List<string> selectedLots, List<string> selectedMatlGrps, List<string> selectedMatlGrpDescs,
            List<string> selectedColors, List<string> selectedOrders, List<string> selectedStations, List<string> selectedOperationActions,
            List<string> selectedLines, List<string> selectedReworkStations, List<string> allReworkStations, string aging)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                List<TimeSpan> ShiftTime = GetShiftTime(shift);
                List<String> AgingRange = GetAgingRange(aging);

                sql = @"
                SELECT VM.*
                FROM (
                (
                SELECT DISTINCT
                    VM.STATION_ID,
                    VM.STATION_NAME,
                    VM.DISPLAY_SEQ,
                    (SUM(VM.IN_LINE) OVER (PARTITION BY VM.STATION_ID) - ISNULL(CAS.CAS_COUNT, 0)) AS IN_LINE,";

                foreach (var rework in allReworkStations)
                {
                    sql += $" SUM(ISNULL(VM.[{rework}], 0)) OVER (PARTITION BY VM.STATION_ID) AS [{rework}],";
                }

                sql += @"
                    (SUM(VM.[COUNT]) OVER (PARTITION BY VM.STATION_ID) - ISNULL(CAS.CAS_COUNT, 0)) AS [TOTAL_WIP],
                    ISNULL(CAS.CAS_COUNT, 0) AS [F2_CASCADE]
                FROM (
                (
                SELECT
	                SM.STATION_ID, SM.STATION_NAME, SM.DISPLAY_SEQ,
	                CASE WHEN (IN_LINE.IN_LINE IS NULL) THEN  0 ELSE IN_LINE.IN_LINE END [IN_LINE],";

                foreach (var rework in allReworkStations)
                {
                    sql += $" 0 AS [{rework}],";
                }

                sql += @"
                    0 AS [F2_CASCADE],
	                CASE WHEN (IN_LINE.IN_LINE IS NULL) THEN 0 ELSE IN_LINE.IN_LINE END AS [COUNT]
                FROM
                (
	                SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
	                WHERE STATUS = 'Y' AND (BROADCAST_FLAG <> 'Y' OR BROADCAST_FLAG IS NULL)
	                AND (LINE_DESC NOT IN ('Rework', 'PBSI') OR LINE_DESC IS NULL)
                ) SM
                LEFT JOIN
                (
	                SELECT
		                SM.STATION_ID, SM.STATION_NAME, SM.DISPLAY_SEQ, COUNT(VM.VIN_NO) AS [IN_LINE],";

                foreach (var rework in allReworkStations)
                {
                    sql += $" 0 AS [{rework}],";
                }

                sql += @"
                    NULL AS [F2_CASCADE],
                    COUNT(VM.VIN_NO) AS [COUNT]
	                FROM (
	                SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
	                WHERE STATUS = 'Y' AND (BROADCAST_FLAG <> 'Y' OR BROADCAST_FLAG IS NULL)
	                AND (LINE_DESC NOT IN ('Rework', 'PBSI') OR LINE_DESC IS NULL)
	                ) SM, VEHICLEMST VM, MODELMST MM
	                WHERE 1=1
                    AND VM.MATL_GROUP = MM.MATL_GROUP
	                AND
	                (
		                (
			                SELECT TOP 1 rt.ACTION
			                FROM REWORK_TIME rt
			                WHERE rt.VIN_NO = VM.VIN_NO
			                ORDER BY rt.DATE_TIME DESC
		                ) = 'TakeIn'
		                OR VM.TAKEOUT_STATION IS NULL
	                )
	                AND SM.STATION_NAME IS NOT NULL
	                AND (VM.OPERATION_ACTION NOT IN ('A070', 'A080'))
	                AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))
	                AND SM.STATION_ID = VM.OPERATION_ID";

                if (!string.IsNullOrEmpty(chassis))
                {
                    SqlParams["@chassis"] = chassis.ToUpper();
                    sql += @" AND VM.CHASSIS_NO LIKE '%' + @chassis + '%'";
                }

                if (!string.IsNullOrEmpty(engineNo))
                {
                    SqlParams["@engineNo"] = engineNo.ToUpper();
                    sql += @" AND UPPER(VM.ENGINE_NO) LIKE '%' + @engineNo + '%'";
                }

                if (datetimeAsOf != DateTime.MinValue)
                {
                    SqlParams["@datetimeAsOf"] = datetimeAsOf;
                    sql += @" AND VM.UPDATE_DATETIME <= @datetimeAsOf";
                }

                if (selectedLots.Count() > 0)
                {
                    sql += " AND VM.WBS_ELEM IN (";
                    for (int i = 0; i < selectedLots.Count(); i++)
                    {
                        SqlParams["@lot" + i] = selectedLots.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != selectedLots.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrpDescs.Count() > 0)
                {
                    sql += " AND MM.MODEL_ID IN (";
                    for (int i = 0; i < selectedMatlGrpDescs.Count(); i++)
                    {
                        SqlParams["@matlGrpDesc" + i] = selectedMatlGrpDescs.ElementAt(i);

                        sql += "@matlGrpDesc" + i;
                        if (i != selectedMatlGrpDescs.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrps.Count() > 0)
                {
                    sql += " AND VM.MATL_GROUP IN (";
                    for (int i = 0; i < selectedMatlGrps.Count(); i++)
                    {
                        SqlParams["@matlGrp" + i] = selectedMatlGrps.ElementAt(i);

                        sql += "@matlGrp" + i;
                        if (i != selectedMatlGrps.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedColors.Count() > 0)
                {
                    sql += " AND VM.COLOR_CODE IN (";
                    for (int i = 0; i < selectedColors.Count(); i++)
                    {
                        SqlParams["@color" + i] = selectedColors.ElementAt(i);

                        sql += "@color" + i;
                        if (i != selectedColors.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedOperationActions.Count() > 0)
                {
                    sql += " AND VM.OPERATION_ACTION IN (";
                    for (int i = 0; i < selectedOperationActions.Count(); i++)
                    {
                        SqlParams["@operationAction" + i] = selectedOperationActions.ElementAt(i);

                        sql += "@operationAction" + i;
                        if (i != selectedOperationActions.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (AgingRange != null && AgingRange.Count == 2)
                {
                    String minAge = AgingRange[0];
                    String maxAge = AgingRange[1];
                    SqlParams["@MIN_AGE"] = minAge;
                    SqlParams["@MAX_AGE"] = maxAge;

                    if (!string.IsNullOrEmpty(minAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) >= @MIN_AGE";
                    }
                    if (!string.IsNullOrEmpty(maxAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) <= @MAX_AGE";
                    }

                }

                if (ShiftTime != null && ShiftTime.Count == 2)
                {
                    TimeSpan startTime = ShiftTime[0];
                    TimeSpan endTime = ShiftTime[1];
                    SqlParams["@START_TIME"] = startTime;
                    SqlParams["@END_TIME"] = endTime;

                    if (startTime < endTime)
                    {
                        sql += @"
                        AND CONVERT(time(7), VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7), VM.UPDATE_DATETIME) < @END_TIME";
                    }
                    else
                    {
                        sql += @"
                        AND
                        (
	                        (CONVERT(time(7), VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7), VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7), VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7), VM.UPDATE_DATETIME) < @END_TIME)
                        )";
                    }
                }

                sql += @"
	                GROUP BY VM.OPERATION_ID, SM.STATION_ID, SM.STATION_NAME, SM.DISPLAY_SEQ
                ) IN_LINE
                ON SM.STATION_ID = IN_LINE.STATION_ID

                UNION

                SELECT DISTINCT
	                REWORK.STATION_ID, REWORK.STATION_NAME, REWORK.DISPLAY_SEQ, REWORK.[IN_LINE], ";

                foreach (var rework in allReworkStations)
                {
                    sql += $"SUM(REWORK.[{rework}]) OVER (PARTITION BY REWORK.STATION_ID) AS [{rework}],";
                }

                sql += @"
                    REWORK.[F2_CASCADE],
	                SUM(REWORK.[COUNT]) OVER (PARTITION BY REWORK.STATION_ID) AS [TOTAL_WIP]
                FROM
                (
	                SELECT SM.STATION_ID, SM.STATION_NAME, SM.DISPLAY_SEQ, 0 AS [IN_LINE],";

                foreach (var rework in allReworkStations)
                {
                    sql += $" CASE WHEN (VM.TAKEOUT_STATION = '{rework}') THEN COUNT(VM.VIN_NO) ELSE 0 END AS [{rework}],";
                }

                sql += @"
	                0 AS [F2_CASCADE], COUNT(VM.VIN_NO) AS [COUNT]
	                FROM VEHICLEMST VM, MODELMST MM, 
	                (
		                SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
		                WHERE STATUS = 'Y' AND (BROADCAST_FLAG <> 'Y' OR BROADCAST_FLAG IS NULL)
		                AND (LINE_DESC NOT IN ('PBSI') OR LINE_DESC IS NULL)
	                ) SM
	                WHERE 1=1
                    AND VM.MATL_GROUP = MM.MATL_GROUP 
	                AND (
		                SELECT TOP 1
		                rt.ACTION
		                FROM REWORK_TIME rt
		                WHERE rt.VIN_NO = VM.VIN_NO
		                ORDER BY rt.DATE_TIME DESC
	                ) <> 'TakeIn'
	                AND VM.TAKEOUT_STATION IS NOT NULL
	                AND SM.STATION_NAME IS NOT NULL
	                AND (VM.OPERATION_ACTION NOT IN ('A070', 'A080')
	                OR VM.OPERATION_ACTION IS NULL)
	                AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))
	                AND SM.STATION_ID = VM.OPERATION_ID";

                if (!string.IsNullOrEmpty(chassis))
                {
                    SqlParams["@chassis"] = chassis.ToUpper();
                    sql += @" AND VM.CHASSIS_NO LIKE '%' + @chassis + '%'";
                }

                if (!string.IsNullOrEmpty(engineNo))
                {
                    SqlParams["@engineNo"] = engineNo.ToUpper();
                    sql += @" AND UPPER(VM.ENGINE_NO) LIKE '%' + @engineNo + '%'";
                }

                if (datetimeAsOf != DateTime.MinValue)
                {
                    SqlParams["@datetimeAsOf"] = datetimeAsOf;
                    sql += @" AND VM.UPDATE_DATETIME <= @datetimeAsOf";
                }

                if (selectedLots.Count() > 0)
                {
                    sql += " AND VM.WBS_ELEM IN (";
                    for (int i = 0; i < selectedLots.Count(); i++)
                    {
                        SqlParams["@lot" + i] = selectedLots.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != selectedLots.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrpDescs.Count() > 0)
                {
                    sql += " AND MM.MODEL_ID IN (";
                    for (int i = 0; i < selectedMatlGrpDescs.Count(); i++)
                    {
                        SqlParams["@matlGrpDesc" + i] = selectedMatlGrpDescs.ElementAt(i);

                        sql += "@matlGrpDesc" + i;
                        if (i != selectedMatlGrpDescs.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrps.Count() > 0)
                {
                    sql += " AND VM.MATL_GROUP IN (";
                    for (int i = 0; i < selectedMatlGrps.Count(); i++)
                    {
                        SqlParams["@matlGrp" + i] = selectedMatlGrps.ElementAt(i);

                        sql += "@matlGrp" + i;
                        if (i != selectedMatlGrps.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedColors.Count() > 0)
                {
                    sql += " AND VM.COLOR_CODE IN (";
                    for (int i = 0; i < selectedColors.Count(); i++)
                    {
                        SqlParams["@color" + i] = selectedColors.ElementAt(i);

                        sql += "@color" + i;
                        if (i != selectedColors.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedOperationActions.Count() > 0)
                {
                    sql += " AND VM.OPERATION_ACTION IN (";
                    for (int i = 0; i < selectedOperationActions.Count(); i++)
                    {
                        SqlParams["@operationAction" + i] = selectedOperationActions.ElementAt(i);

                        sql += "@operationAction" + i;
                        if (i != selectedOperationActions.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (AgingRange != null && AgingRange.Count == 2)
                {
                    String minAge = AgingRange[0];
                    String maxAge = AgingRange[1];
                    SqlParams["@MIN_AGE"] = minAge;
                    SqlParams["@MAX_AGE"] = maxAge;

                    if (!string.IsNullOrEmpty(minAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) >= @MIN_AGE";
                    }
                    if (!string.IsNullOrEmpty(maxAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) <= @MAX_AGE";
                    }

                }

                if (ShiftTime != null && ShiftTime.Count == 2)
                {
                    TimeSpan startTime = ShiftTime[0];
                    TimeSpan endTime = ShiftTime[1];
                    SqlParams["@START_TIME"] = startTime;
                    SqlParams["@END_TIME"] = endTime;

                    if (startTime < endTime)
                    {
                        sql += @"
                        AND CONVERT(time(7), VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7), VM.UPDATE_DATETIME) < @END_TIME";
                    }
                    else
                    {
                        sql += @"
                        AND
                        (
	                        (CONVERT(time(7), VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7), VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7), VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7), VM.UPDATE_DATETIME) < @END_TIME)
                        )";
                    }
                }

                sql += @"
	                GROUP BY SM.STATION_ID, SM.STATION_NAME, SM.DISPLAY_SEQ, VM.TAKEOUT_STATION
                ) REWORK
                )
                ) VM

                LEFT JOIN (
                SELECT COUNT(CAS.VIN_NO) AS [CAS_COUNT], CAS.OPERATION_ID
                FROM (
	                SELECT DISTINCT VM.VIN_NO, VM.OPERATION_ID,
	                CASE WHEN (GETDATE() > vm.CASCADE_DATETIME AND DATEDIFF(MINUTE, vm.CASCADE_DATETIME, GETDATE()) > 5) THEN VM.CASCADE_DATETIME ELSE NULL END AS CASCADE_DATETIME
	                FROM VEHICLEMST VM, MODELMST MM
	                WHERE VM.CASCADE_DATETIME IS NOT NULL
                    AND VM.MATL_GROUP = MM.MATL_GROUP
	                AND VM.OPERATION_ACTION NOT IN ('A070', 'A080')
	                AND VM.OPERATION_ID NOT IN (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE MAIN_SUB = 'SUB')
	                AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

                if (!string.IsNullOrEmpty(chassis))
                {
                    SqlParams["@chassis"] = chassis.ToUpper();
                    sql += @" AND VM.CHASSIS_NO LIKE '%' + @chassis + '%'";
                }

                if (!string.IsNullOrEmpty(engineNo))
                {
                    SqlParams["@engineNo"] = engineNo.ToUpper();
                    sql += @" AND UPPER(VM.ENGINE_NO) LIKE '%' + @engineNo + '%'";
                }

                if (selectedLots.Count() > 0)
                {
                    sql += " AND VM.WBS_ELEM IN (";
                    for (int i = 0; i < selectedLots.Count(); i++)
                    {
                        SqlParams["@lot" + i] = selectedLots.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != selectedLots.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrpDescs.Count() > 0)
                {
                    sql += " AND MM.MODEL_ID IN (";
                    for (int i = 0; i < selectedMatlGrpDescs.Count(); i++)
                    {
                        SqlParams["@matlGrpDesc" + i] = selectedMatlGrpDescs.ElementAt(i);

                        sql += "@matlGrpDesc" + i;
                        if (i != selectedMatlGrpDescs.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrps.Count() > 0)
                {
                    sql += " AND VM.MATL_GROUP IN (";
                    for (int i = 0; i < selectedMatlGrps.Count(); i++)
                    {
                        SqlParams["@matlGrp" + i] = selectedMatlGrps.ElementAt(i);

                        sql += "@matlGrp" + i;
                        if (i != selectedMatlGrps.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedColors.Count() > 0)
                {
                    sql += " AND VM.COLOR_CODE IN (";
                    for (int i = 0; i < selectedColors.Count(); i++)
                    {
                        SqlParams["@color" + i] = selectedColors.ElementAt(i);

                        sql += "@color" + i;
                        if (i != selectedColors.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedOperationActions.Count() > 0)
                {
                    sql += " AND VM.OPERATION_ACTION IN (";
                    for (int i = 0; i < selectedOperationActions.Count(); i++)
                    {
                        SqlParams["@operationAction" + i] = selectedOperationActions.ElementAt(i);

                        sql += "@operationAction" + i;
                        if (i != selectedOperationActions.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (AgingRange != null && AgingRange.Count == 2)
                {
                    String minAge = AgingRange[0];
                    String maxAge = AgingRange[1];
                    SqlParams["@MIN_AGE"] = minAge;
                    SqlParams["@MAX_AGE"] = maxAge;

                    if (!string.IsNullOrEmpty(minAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) >= @MIN_AGE";
                    }
                    if (!string.IsNullOrEmpty(maxAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) <= @MAX_AGE";
                    }

                }

                sql += @"
	                GROUP BY VM.OPERATION_ID, VM.VIN_NO, VM.CASCADE_DATETIME
                ) CAS
                WHERE CAS.CASCADE_DATETIME IS NOT NULL
                GROUP BY CAS.OPERATION_ID
                ) CAS
                ON VM.STATION_ID = CAS.OPERATION_ID
                GROUP BY VM.STATION_ID, VM.STATION_NAME, VM.DISPLAY_SEQ, VM.IN_LINE, ";

                foreach (var rework in allReworkStations)
                {
                    sql += $"VM.[{rework}],";
                }

                sql += @"
                VM.F2_CASCADE, VM.[COUNT], CAS.CAS_COUNT
                )) VM
                WHERE 1 = 1";

                if (selectedStations.Count() > 0)
                {
                    sql += " AND VM.STATION_ID IN (";
                    for (int i = 0; i < selectedStations.Count(); i++)
                    {
                        SqlParams["@station" + i] = selectedStations.ElementAt(i);

                        sql += "@station" + i;
                        if (i != selectedStations.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedLines.Count() > 0)
                {
                    sql += " AND VM.STATION_ID IN (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE STATION_GROUP IN (";
                    for (int i = 0; i < selectedLines.Count(); i++)
                    {
                        SqlParams["@line" + i] = selectedLines.ElementAt(i);

                        sql += "@line" + i;
                        if (i != selectedLines.Count() - 1) sql += ",";
                    }
                    sql += "))";
                }

                if (selectedReworkStations.Count() > 0)
                {
                    sql += " AND (";
                    for (int i = 0; i < selectedReworkStations.Count(); i++)
                    {
                        string rework = selectedReworkStations.ElementAt(i);
                        sql += $"(VM.[{rework}] IS NOT NULL AND VM.[{rework}] <> 0)";
                        if (i != selectedReworkStations.Count() - 1) sql += " OR ";
                    }
                    sql += ")";
                }

                if (!string.IsNullOrEmpty(chassis) || !string.IsNullOrEmpty(engineNo))
                {
                    sql += " AND (VM.IN_LINE <> 0 OR ";
                    for (int i = 0; i < allReworkStations.Count(); i++)
                    {
                        string rework = allReworkStations.ElementAt(i);
                        sql += $"VM.[{rework}] <> 0";
                        if (i != allReworkStations.Count() - 1) sql += " OR ";
                    }
                    sql += ")";
                }

                sql += @" ORDER BY DISPLAY_SEQ ";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetPBSIWIPInfo(DateTime datetimeAsOf, List<string> selectedLots, List<string> selectedMatlGrps, List<string> selectedMatlGrpDescs,
            List<string> selectedLines, List<string> allReworkStations, string shift, string aging, string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                List<TimeSpan> ShiftTime = GetShiftTime(shift);
                List<String> AgingRange = GetAgingRange(aging);

                sql = @"
                SELECT
                    QR.VIN_NO, '-' AS CHASSIS_NO, '-' AS [ORDER], 0 AS SEQ_IN_LOT, QR.MATERIAL_GROUP_DESC AS MATL_GROUP_DESC,
                    QR.MODEL_ID, QR.MATERIAL_GROUP AS MATL_GROUP, '-' AS ENGINE_NO, QR.LOT_NO AS WBS_ELEM, '-' AS COLOR_CODE, '-' AS DESTINATION,
                    QR.AG_INTERIOR_COLOR, QR.AG_EXTERIOR_COLOR,
                    SM.STATION_ID, SM.STATION_NAME, '-' AS OPERATION_ACTION, '-' AS F2_CASCADE, '-' AS REWORK, QR.UPDATE_DATETIME,
                    NULL AS SAP_DATETIME,
                    DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) AS AGING,
                    '-' AS BLOCK_FLAG,
                    NULL AS LINEOFF_DATETIME
,NULL AS MATCHED_DATETIME
                FROM
                (
	                SELECT QR.VIN_NO, QR.MODEL_ID, QR.MATERIAL_GROUP, QR.MATERIAL_GROUP_DESC, QR.LOT_NO,
	                QR.AG_EXTERIOR_COLOR, QR.AG_INTERIOR_COLOR, QR.UPDATE_DATETIME, QR.PRINT_DATETIME,
	                (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE LINE_DESC = 'PBSI') AS STATION_ID
	                FROM QRINFO QR
	                LEFT JOIN (SELECT VH.VIN_NO FROM (SELECT VIN_NO, COUNT(VIN_NO) AS TOTAL FROM VEHICLE_HISTORY GROUP BY VIN_NO) VH WHERE VH.TOTAL = 1) VH
	                ON QR.VIN_NO = VH.VIN_NO
	                WHERE QR.VIN_NO = VH.VIN_NO
                ) QR
                LEFT JOIN (SELECT DISTINCT STATION_ID, STATION_NAME, LINE_DESC FROM STATIONMST) SM
                ON QR.STATION_ID = SM.STATION_ID
                LEFT JOIN VEHICLEMST VM
                ON VM.VIN_NO = QR.VIN_NO
                LEFT JOIN MODELMST MM
                ON QR.MATERIAL_GROUP = MM.MATL_GROUP
                WHERE SM.LINE_DESC = 'PBSI' ";

                if (datetimeAsOf != DateTime.MinValue)
                {
                    SqlParams["@datetimeAsOf"] = datetimeAsOf;
                    sql += @" AND QR.UPDATE_DATETIME < @datetimeAsOf";
                }

                if (selectedLots.Count() > 0)
                {
                    sql += " AND QR.LOT_NO IN (";
                    for (int i = 0; i < selectedLots.Count(); i++)
                    {
                        SqlParams["@lot" + i] = selectedLots.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != selectedLots.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrpDescs.Count() > 0)
                {
                    sql += " AND QR.MODEL_ID IN (";
                    for (int i = 0; i < selectedMatlGrpDescs.Count(); i++)
                    {
                        SqlParams["@matlGrpDesc" + i] = selectedMatlGrpDescs.ElementAt(i);

                        sql += "@matlGrpDesc" + i;
                        if (i != selectedMatlGrpDescs.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrps.Count() > 0)
                {
                    sql += " AND QR.MATERIAL_GROUP IN (";
                    for (int i = 0; i < selectedMatlGrps.Count(); i++)
                    {
                        SqlParams["@matlGrp" + i] = selectedMatlGrps.ElementAt(i);

                        sql += "@matlGrp" + i;
                        if (i != selectedMatlGrps.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedLines.Count() > 0)
                {
                    sql += " AND MM.LINE IN (";
                    for (int i = 0; i < selectedLines.Count(); i++)
                    {
                        SqlParams["@line" + i] = selectedLines.ElementAt(i);

                        sql += "@line" + i;
                        if (i != selectedLines.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (AgingRange != null && AgingRange.Count == 2)
                {
                    String minAge = AgingRange[0];
                    String maxAge = AgingRange[1];
                    SqlParams["@MIN_AGE"] = minAge;
                    SqlParams["@MAX_AGE"] = maxAge;

                    if (!string.IsNullOrEmpty(minAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) >= @MIN_AGE";
                    }
                    if (!string.IsNullOrEmpty(maxAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) <= @MAX_AGE";
                    }

                }

                if (ShiftTime != null && ShiftTime.Count == 2)
                {
                    TimeSpan startTime = ShiftTime[0];
                    TimeSpan endTime = ShiftTime[1];
                    SqlParams["@START_TIME"] = startTime;
                    SqlParams["@END_TIME"] = endTime;

                    if (startTime < endTime)
                    {
                        sql += @"
                        AND CONVERT(time(7), QR.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7), QR.UPDATE_DATETIME) < @END_TIME";
                    }
                    else
                    {
                        sql += @"
                        AND
                        (
	                        (CONVERT(time(7), QR.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7), QR.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7), QR.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7), QR.UPDATE_DATETIME) < @END_TIME)
                        )";
                    }
                }

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetPBSIWipCount(DateTime datetimeAsOf, List<string> selectedLots, List<string> selectedMatlGrps, List<string> selectedMatlGrpDescs,
            List<string> selectedLines, List<string> allReworkStations, string shift, string aging, string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                List<TimeSpan> ShiftTime = GetShiftTime(shift);
                List<String> AgingRange = GetAgingRange(aging);

                sql = @"
                SELECT
	                (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE LINE_DESC = 'PBSI') AS STATION_ID,
	                (SELECT DISTINCT STATION_NAME FROM STATIONMST WHERE LINE_DESC = 'PBSI') AS STATION_NAME,
	                (SELECT DISTINCT DISPLAY_SEQ FROM STATIONMST WHERE LINE_DESC = 'PBSI') AS DISPLAY_SEQ,
	                COUNT(VH.VIN_NO) AS [IN_LINE], ";

                foreach (var rework in allReworkStations)
                {
                    sql += $"0 AS [{rework}],";
                }

                sql += @"
                    (COUNT(VH.VIN_NO)) AS [TOTAL_WIP], 0 AS [F2_CASCADE]
	                FROM QRINFO QR, MODELMST mm,
	                (
	                    SELECT DISTINCT VH.VIN_NO, VH.UPDATE_DATETIME FROM
	                    (SELECT VIN_NO, MAX(UPDATE_DATETIME) AS UPDATE_DATETIME, COUNT(VIN_NO) AS TOTAL FROM VEHICLE_HISTORY GROUP BY VIN_NO) VH WHERE VH.TOTAL = 1
	                ) VH
                    LEFT JOIN VEHICLEMST VM ON VM.VIN_NO = VH.VIN_NO
	                WHERE 1=1
	                AND VH.VIN_NO IS NOT NULL
	                AND VH.VIN_NO = QR.VIN_NO
	                AND mm.MATL_GROUP = QR.MATERIAL_GROUP
	                AND mm.MODEL_ID = QR.MODEL_ID
	                AND (
	                (
		                SELECT TOP 1 rt1.ACTION
		                FROM REWORK_TIME rt1
		                WHERE rt1.VIN_NO = VH.VIN_NO
		                ORDER BY rt1.DATE_TIME DESC
	                ) = 'TakeIn'
	                OR VH.VIN_NO IS NOT NULL) ";

                if (datetimeAsOf != DateTime.MinValue)
                {
                    SqlParams["@datetimeAsOf"] = datetimeAsOf;
                    sql += @" AND VH.UPDATE_DATETIME <= @datetimeAsOf";
                }

                if (selectedLots.Count() > 0)
                {
                    sql += " AND QR.LOT_NO IN (";
                    for (int i = 0; i < selectedLots.Count(); i++)
                    {
                        SqlParams["@lot" + i] = selectedLots.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != selectedLots.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrpDescs.Count() > 0)
                {
                    sql += " AND QR.MODEL_ID IN (";
                    for (int i = 0; i < selectedMatlGrpDescs.Count(); i++)
                    {
                        SqlParams["@matlGrpDesc" + i] = selectedMatlGrpDescs.ElementAt(i);

                        sql += "@matlGrpDesc" + i;
                        if (i != selectedMatlGrpDescs.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedMatlGrps.Count() > 0)
                {
                    sql += " AND QR.MATERIAL_GROUP IN (";
                    for (int i = 0; i < selectedMatlGrps.Count(); i++)
                    {
                        SqlParams["@matlGrp" + i] = selectedMatlGrps.ElementAt(i);

                        sql += "@matlGrp" + i;
                        if (i != selectedMatlGrps.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (selectedLines.Count() > 0)
                {
                    sql += " AND MM.LINE IN (";
                    for (int i = 0; i < selectedLines.Count(); i++)
                    {
                        SqlParams["@line" + i] = selectedLines.ElementAt(i);

                        sql += "@line" + i;
                        if (i != selectedLines.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                if (AgingRange != null && AgingRange.Count == 2)
                {
                    String minAge = AgingRange[0];
                    String maxAge = AgingRange[1];
                    SqlParams["@MIN_AGE"] = minAge;
                    SqlParams["@MAX_AGE"] = maxAge;

                    if (!string.IsNullOrEmpty(minAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) >= @MIN_AGE";
                    }
                    if (!string.IsNullOrEmpty(maxAge))
                    {
                        sql += @" AND DATEDIFF(DAY, VM.MATCHED_DATETIME, GETDATE()) <= @MAX_AGE";
                    }

                }

                if (ShiftTime != null && ShiftTime.Count == 2)
                {
                    TimeSpan startTime = ShiftTime[0];
                    TimeSpan endTime = ShiftTime[1];
                    SqlParams["@START_TIME"] = startTime;
                    SqlParams["@END_TIME"] = endTime;

                    if (startTime < endTime)
                    {
                        sql += @"
                        AND CONVERT(time(7), QR.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7), QR.UPDATE_DATETIME) < @END_TIME";
                    }
                    else
                    {
                        sql += @"
                        AND
                        (
	                        (CONVERT(time(7), QR.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7), QR.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7), QR.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7), QR.UPDATE_DATETIME) < @END_TIME)
                        )";
                    }
                }

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllShifts(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"SELECT DISTINCT SHIFT_ID, SHIFT_NAME FROM SHIFTMST";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllLots(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"
                SELECT DISTINCT WBS_ELEM AS LOT FROM VEHICLEMST
                UNION
                SELECT QR.LOT_NO AS LOT FROM QRINFO QR, VEHICLE_HISTORY VH
                WHERE QR.VIN_NO = VH.VIN_NO
                AND ((SELECT COUNT(VIN_NO) FROM VEHICLEMST WHERE VIN_NO = QR.VIN_NO AND VIN_NO = VH.VIN_NO) = 0)
                ORDER BY WBS_ELEM";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllMatlGroups(string userId, List<string> lot)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"
                SELECT DISTINCT VM.MATL_GROUP
                FROM
                (
	                SELECT WBS_ELEM AS LOT_NO, MATL_GROUP FROM VEHICLEMST
	                UNION
	                SELECT QR.LOT_NO, QR.MATERIAL_GROUP AS MATL_GROUP
	                FROM QRINFO QR, VEHICLE_HISTORY VH, MODELMST MM
	                WHERE QR.VIN_NO = VH.VIN_NO
	                AND QR.MATERIAL_GROUP = MM.MATL_GROUP
	                AND QR.MODEL_ID = MM.MODEL_ID
	                AND ((SELECT COUNT(*) FROM VEHICLEMST WHERE VIN_NO = QR.VIN_NO AND VIN_NO = VH.VIN_NO)=0)
                ) VM";

                if (lot.Count() > 0)
                {
                    sql += " AND VM.LOT_NO IN (";
                    for (int i = 0; i < lot.Count(); i++)
                    {
                        SqlParams["@lot" + i] = lot.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != lot.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                sql += " ORDER BY VM.MATL_GROUP ASC ";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllModels(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"
                SELECT DISTINCT MM.MODEL_ID
                FROM MODELMST MM, VEHICLEMST VM
                WHERE MM.MATL_GROUP = VM.MATL_GROUP 
                ORDER BY MM.MODEL_ID ASC ";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllMatlGroupsDesc(string userId, List<string> lot)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"
                SELECT DISTINCT VM.MATL_GROUP_DESC
                FROM
                (
                    SELECT WBS_ELEM AS LOT_NO, MATL_GROUP_DESC FROM VEHICLEMST
                    UNION
                    SELECT QR.LOT_NO, QR.MATERIAL_GROUP_DESC AS MATL_GROUP_DESC
                    FROM QRINFO QR, VEHICLE_HISTORY VH, MODELMST MM
                    WHERE QR.VIN_NO = VH.VIN_NO
                    AND QR.MATERIAL_GROUP = MM.MATL_GROUP
                    AND QR.MODEL_ID = MM.MODEL_ID
                    AND ((SELECT COUNT(*) FROM VEHICLEMST WHERE VIN_NO = QR.VIN_NO AND VIN_NO = VH.VIN_NO)=0)
                ) VM";

                if (lot.Count() > 0)
                {
                    sql += " AND VM.LOT_NO IN (";
                    for (int i = 0; i < lot.Count(); i++)
                    {
                        SqlParams["@lot" + i] = lot.ElementAt(i);

                        sql += "@lot" + i;
                        if (i != lot.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                sql += " ORDER BY VM.MATL_GROUP_DESC ASC ";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllColorCodes(string userId, List<string> matlGroups)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = "SELECT DISTINCT COLOR_CODE FROM VEHICLEMST WHERE 1=1 ";

                if (matlGroups.Count() > 0)
                {
                    sql += " AND MATL_GROUP IN (";
                    for (int i = 0; i < matlGroups.Count(); i++)
                    {
                        SqlParams["@matlGroup" + i] = matlGroups.ElementAt(i);

                        sql += "@matlGroup" + i;
                        if (i != matlGroups.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                sql += " ORDER BY COLOR_CODE ASC ";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllProdOrders(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"SELECT DISTINCT [ORDER] AS PROD_ORDER FROM VEHICLEMST ORDER BY [ORDER]";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllOperationActions(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"
                SELECT DISTINCT OPERATION_ACTION
                FROM STATIONMST WHERE STATUS = 'Y'
                AND OPERATION_ACTION IS NOT NULL";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllLines(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"SELECT DISTINCT LINE_ID, LINE_NAME FROM LINE WHERE STATUS = 'Y' ORDER BY LINE_ID ASC";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllStations(string userId, List<string> lines)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"
                SELECT DISTINCT STATION_ID, STATION_NAME, SORTING_SEQ
                FROM STATIONMST WHERE STATUS = 'Y'
                AND (BROADCAST_FLAG IS NULL OR BROADCAST_FLAG = 'N')
                AND (LINE_DESC NOT IN ('REWORK') OR LINE_DESC IS NULL )";

                if (lines.Count() > 0)
                {
                    sql += " AND STATION_GROUP IN (";
                    for (int i = 0; i < lines.Count(); i++)
                    {
                        SqlParams["@line" + i] = lines.ElementAt(i);

                        sql += "@line" + i;
                        if (i != lines.Count() - 1) sql += ",";
                    }
                    sql += ")";
                }

                sql += "ORDER BY SORTING_SEQ";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllReworkStations(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                sql = @"
                SELECT DISTINCT STATION_ID, STATION_NAME, SORTING_SEQ
                FROM STATIONMST WHERE STATUS = 'Y'
                AND LINE_DESC IN ('REWORK')
                ORDER BY SORTING_SEQ";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }

        public DataTable GetAllAging(string userId)
        {
            CommonMethod common = new CommonMethod();

            DatabaseAccessorMSSQL daSQL = new DatabaseAccessorMSSQL();
            Hashtable SqlParams = new Hashtable();

            string methodName = common.MethodName();
            string sql = string.Empty;

            try
            {
                //DataTable dt = new DataTable();

                //dt.Columns.Add("AGING_ID", typeof(string));
                //dt.Columns.Add("AGING_NAME", typeof(string));

                //dt.Rows.Add("AGING001", "> 90 days");
                //dt.Rows.Add("AGING002", "61 - 90 days");
                //dt.Rows.Add("AGING003", "31 - 60 days");
                //dt.Rows.Add("AGING004", "15 - 30 days");
                //dt.Rows.Add("AGING005", "<= 14 days");

                sql = @"SELECT DISTINCT AGING_ID, AGING_DESC FROM AGINGMST";

                DataTable dt = daSQL.ExecuteQuery(methodName, userId, sql, SqlParams, WEB_TYPE);

                return dt;
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }


        public DataTable getAllWipInfo(DateTime from, string Lot, string Model, string Color, string BasicCode, string ProdOrder, string FromStation, string chassisno, string OpAction, string lineId, string Shift, string engineNo, string rework)
        {
            List<TimeSpan> ShiftTime = GetShiftTime(Shift);
            Hashtable hashtable = new Hashtable();

            DataTable dt = null;

            string sql = "";

            sql += @"
            SELECT
                allData.BODY_NO,
                allData.CHASSIS_NO,
                allData.PROD_ORDER,
                allData.PROD_SEQ,
                allData.MSC,
                allData.MODEL,
                allData.MATL_GROUP,
                allData.ENGINE_NO,
                allData.LOT,
                allData.COLOR,
                allData.AG_INTERIOR_COLOR,
                allData.AG_EXTERIOR_COLOR,
                --allData.AGING,
                allData.Station,
                allData.F2_CASCADE,
                allData.REWORK,
                allData.MATCH_DATE_TIME AS SCANNED_DATE_TIME,
                allData.STATION_ID,
                allData.OPERATION_ACTION,
                allData.SAP_ACTION_DATETIME,
                CASE WHEN (AG.CHASSIS_NO IS NOT NULL) THEN 'Y' ELSE '-' END AS BLOCK_FLAG
            FROM
            (
            SELECT
	            qr_data.VIN_NO AS BODY_NO,
	            '-' AS CHASSIS_NO,
	            '-' AS PROD_ORDER,
	            0 AS PROD_SEQ,
	            qr_data.MATERIAL_GROUP_DESC AS MSC,
	            qr_data.MODEL_ID AS MODEL,
	            qr_data.MATERIAL_GROUP AS MATL_GROUP,
	            qr_data.LOT_NO AS LOT,
	            '-' AS COLOR,
	            qr_data.AG_INTERIOR_COLOR,
	            qr_data.AG_EXTERIOR_COLOR,
	            DATEDIFF(MINUTE, qr_data.UPDATE_DATETIME, GETDATE()) AS AGING,
	            sm.STATION_NAME AS Station,
	            '-' AS F2_CASCADE,
	            '-' AS REWORK,
	            qr_data.UPDATE_DATETIME AS MATCH_DATE_TIME,
	            sm.STATION_ID,
	            '-' AS OPERATION_ACTION,
	            NULL AS SAP_ACTION_DATETIME,
	            '-' AS ENGINE_NO
            FROM
            (
	            SELECT
		            qr.*,
		            'Q01' AS STATION_ID
	            FROM
	            (
		            SELECT qr.*
		            FROM QRINFO qr
		            LEFT JOIN
		            (
                        SELECT DISTINCT vh.VIN_NO FROM
                            (SELECT VIN_NO, COUNT(VIN_NO) AS [COUNT] FROM VEHICLE_HISTORY GROUP BY VIN_NO) vh
                        WHERE vh.COUNT = 1
                    ) vh
                    ON qr.VIN_NO = vh.VIN_NO
                    WHERE qr.VIN_NO IS NOT NULL
                    AND qr.VIN_NO != ''
                    AND qr.VIN_NO = vh.VIN_NO
                )qr
            )qr_data
            LEFT JOIN (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST) sm
            ON qr_data.STATION_ID = sm.STATION_ID
            LEFT JOIN VEHICLEMST vm
			ON vm.VIN_NO = qr_data.VIN_NO
            WHERE qr_data.STATION_ID = 'Q01'";

            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE " + db.GetStr(chassisno);
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE " + db.GetStr(engineNo);
            }

            sql += @"
            UNION

            SELECT VM.VIN_NO AS BODY_NO, VM.CHASSIS_NO, VM.[ORDER] AS PROD_ORDER, VM.SEQ_IN_LOT AS PROD_SEQ, VM.MATL_GROUP_DESC AS MSC,
            mm.MODEL_ID AS MODEL, VM.MODEL_NO AS MATL_GROUP, VM.WBS_ELEM AS LOT, VM.COLOR_CODE AS COLOR,
            (SELECT AG_INTERIOR_COLOR FROM QRINFO WHERE VM.VIN_NO = VIN_NO) AS AG_INTERIOR_COLOR, VM.COLOR_DESC AS AG_EXTERIOR_COLOR,
            DATEDIFF(MINUTE, VM.UPDATE_DATETIME, GETDATE()) AS AGING, (SELECT DISTINCT STATION_NAME FROM STATIONMST WHERE VM.OPERATION_ID = [STATION_ID]) AS STATION,
            CASE WHEN (GETDATE() > vm.CASCADE_DATETIME AND DATEDIFF(MINUTE, vm.CASCADE_DATETIME, GETDATE()) > 5) THEN 'Y' ELSE 'N' END AS 'F2_CASCADE',
            CASE WHEN EXISTS
            (
                SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST sm WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                AND (SELECT TOP 1 rt1.ACTION FROM REWORK_TIME rt1
                WHERE rt1.VIN_NO = vm.VIN_NO ORDER BY rt1.DATE_TIME DESC) <> 'TakeIn'
                AND vm.TAKEOUT_STATION IS NOT NULL
                AND sm.STATION_NAME IS NOT NULL
            )
            THEN
            (
                SELECT DISTINCT sm.STATION_NAME
                FROM (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST) sm
                WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                AND (SELECT TOP 1 rt1.ACTION
                FROM REWORK_TIME rt1
                WHERE rt1.VIN_NO = vm.VIN_NO
                ORDER BY rt1.DATE_TIME DESC)
                <> 'TakeIn'
                AND vm.TAKEOUT_STATION IS NOT NULL
                AND sm.STATION_NAME IS NOT NULL
            )
            ELSE '-'
            END AS REWORK,
            CONVERT(VARCHAR(24),VM.UPDATE_DATETIME,120) AS MATCH_DATE_TIME,
            VM.OPERATION_ID AS STATION_ID, vm.OPERATION_ACTION,
            (SELECT TOP 1 DATETIME FROM SAPACTIONHISTORY WHERE CHASSIS_NO = vm.CHASSIS_NO AND OPERATION_ACTION = vm.OPERATION_ACTION ORDER BY DATETIME DESC) AS SAP_ACTION_DATETIME,
            VM.ENGINE_NO
            FROM VEHICLEMST VM, MODELMST mm
            WHERE VM.VIN_NO = VM.VIN_NO
            AND VM.UPDATE_DATETIME <= " + db.GetDateTime(from);

            #region Old Code

            // +" AND " + db.GetDateTime(to); //Comment by KCC 20170222
            /*if (Lot != "ALL" && Lot != "")
sql += " AND VM.WBS_ELEM IN ( " + Lot + " ) ";//sql += " AND VM.WBS_ELEM =" + db.GetStr(Lot);
if (Model != "ALL" && Model != "")
sql += " AND VM.MODEL_NO IN ( " + Model + " ) ";//sql += " AND VM.MODEL_NO =" + db.GetStr(Model);
if (Color != "ALL" && Color != "")
sql += " AND VM.COLOR_CODE IN ( " + Color + " ) ";//sql += " AND VM.COLOR_CODE =" + db.GetStr(Color);
if (BasicCode != "ALL" && BasicCode != "")
sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";//sql += " AND VM.MATL_GROUP_DESC =" + db.GetStr(BasicCode);
if (ProdOrder != "ALL" && ProdOrder != "")
sql += " AND VM.[ORDER] IN ( " + ProdOrder + " ) "; //sql += " AND VM.[ORDER] LIKE "+ db.GetStr(ProdOrder) (10/6/19);
if (FromStation != "")
{
sql += " AND VH.STATION_ID BETWEEN " + db.GetStr(FromStation) + " AND " + db.GetStr(ToStation);
sql += " AND VM.OPERATION_ID IN ";
sql += " ( ";
sql += "    SELECT DISTINCT sm.STATION_ID  ";
sql += "    FROM STATIONMST sm ";
sql += "    WHERE sm.SORTING_SEQ BETWEEN " + db.GetStr(FromStation) + " AND " + db.GetStr(ToStation);
sql += "    WHERE sm.STATION_ID IN ( ";
sql += "               SELECT DISTINCT mm2.STATION_ID ";
sql += "               FROM STATIONMST mm2 ";
sql += "               WHERE mm2.STATION_ID IN (" + FromStation + ")";// + " AND " + db.GetStr(ToStation);
sql += "               AND (mm2.BROADCAST_FLAG IS NULL OR mm2.BROADCAST_FLAG = 'N') ";

if (lineId != "")
{
sql += " AND mm2.STATION_GROUP IN (" + lineId + ") "; //sql += " AND mm2.STATION_GROUP = " + db.GetStr(lineId); (10/6/19)
}

sql += "            )";
sql += " ) ";
}
if (OpAction != "ALL" && OpAction != "")
sql += " AND VM.OPERATION_ACTION IN ( " + OpAction + " ) ";//sql += " AND VM.OPERATION_ACTION = " + db.GetStr(OpAction);
                                                    if (chassisno != "ALL") //Comment by KCC 20170221*/

            #endregion Old Code

            sql += " AND vm.MODEL_NO = mm.MATL_GROUP ";
            sql += " AND VM.CHASSIS_NO LIKE " + db.GetStr(chassisno);
            sql += " AND VM.ENGINE_NO LIKE " + db.GetStr(engineNo);

            /* if (lineId != "")
             {
                 sql += " AND mm.LINE IN (" + lineId + ") "; //sql += " AND mm.LINE = " + db.GetStr(lineId); (10/6/19)
             }*/

            sql += @"
            ) allData
            LEFT JOIN (SELECT DISTINCT CHASSIS_NO FROM AG_BLOCK) AG
            ON allData.CHASSIS_NO = AG.CHASSIS_NO
            LEFT JOIN MODELMST mm
            ON mm.MODEL_ID = allData.MODEL
            AND mm.MATL_GROUP = allData.MATL_GROUP
            WHERE allData.OPERATION_ACTION NOT IN ('A070', 'A080')
            AND allData.CHASSIS_NO NOT IN (SELECT DISTINCT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (rework != "All" && rework != "")
            {
                sql += " AND allData.REWORK IN ( " + rework + " ) ";
            }

            if (FromStation != "")
            {
                sql += @"
AND allData.STATION_ID IN
(
     SELECT DISTINCT sm.STATION_ID FROM STATIONMST sm WHERE sm.STATION_ID IN
    (SELECT DISTINCT mm2.STATION_ID    FROM STATIONMST mm2   WHERE mm2.STATION_ID IN ('') AND (mm2.BROADCAST_FLAG IS NULL OR mm2.BROADCAST_FLAG = 'N'))
)
";
                sql += " AND allData.STATION_ID IN ";
                sql += " ( ";
                sql += "    SELECT DISTINCT sm.STATION_ID  ";
                sql += "    FROM STATIONMST sm ";
                sql += "    WHERE sm.STATION_ID IN ( ";
                sql += "               SELECT DISTINCT mm2.STATION_ID ";
                sql += "               FROM STATIONMST mm2 ";
                sql += "               WHERE mm2.STATION_ID IN (" + FromStation + ")";
                sql += "               AND (mm2.BROADCAST_FLAG IS NULL OR mm2.BROADCAST_FLAG = 'N') ";

                if (lineId != "")
                {
                    sql += " AND mm2.STATION_GROUP IN (" + lineId + ") ";
                }

                sql += "            )";
                sql += " ) ";
            }

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND allData.LOT IN ( " + Lot + " ) ";
            }

            if (Model != "ALL" && Model != "")
            {
                sql += " AND allData.MATL_GROUP IN ( " + Model + " ) ";
            }

            if (Color != "ALL" && Color != "")
            {
                sql += " AND allData.COLOR IN ( " + Color + " ) ";
            }

            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND allData.MSC IN ( " + BasicCode + " ) ";
            }

            if (ProdOrder != "ALL" && ProdOrder != "")
            {
                sql += " AND allData.PROD_ORDER IN ( " + ProdOrder + " ) ";
            }

            if (OpAction != "ALL" && OpAction != "")
            {
                sql += " AND allData.OPERATION_ACTION IN ( " + OpAction + " ) ";
            }

            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }

            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),allData.MATCH_DATE_TIME) >= @START_TIME
                        AND CONVERT(time(7),allData.MATCH_DATE_TIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),allData.MATCH_DATE_TIME) >= @START_TIME AND CONVERT(time(7),allData.MATCH_DATE_TIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),allData.MATCH_DATE_TIME) >= '00:00:00.0000000' AND CONVERT(time(7),allData.MATCH_DATE_TIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += " ORDER BY allData.OPERATION_ACTION DESC, allData.SAP_ACTION_DATETIME DESC, allData.MATCH_DATE_TIME DESC  ";

            return retrieveData(sql, hashtable);
        }

        public DataTable getAllWipInfoNoDate(string Lot, string Model, string Color, string BasicCode, string ProdOrder, string FromStation, string chassisno, string OpAction, string lineId, string Shift, string engineNo, string rework) //Bo Jun add OpAction
        {
            List<TimeSpan> ShiftTime = GetShiftTime(Shift);
            Hashtable hashtable = new Hashtable();

            DataTable dt = null;

            string sql = "";

            sql += @" SELECT
                        allData.BODY_NO,
                        allData.CHASSIS_NO,
                        allData.PROD_ORDER,
                        allData.PROD_SEQ,
                        allData.MSC,
                        allData.MODEL,
                        allData.MATL_GROUP,
                        allData.ENGINE_NO,
                        allData.LOT,
                        allData.COLOR,
                        allData.AG_INTERIOR_COLOR,
                        allData.AG_EXTERIOR_COLOR,
                        --allData.AGING,
                        allData.Station,
                        allData.F2_CASCADE,
                        allData.REWORK,
                        allData.MATCH_DATE_TIME AS SCANNED_DATE_TIME,
                        allData.STATION_ID,
                        allData.OPERATION_ACTION,
                        allData.SAP_ACTION_DATETIME
                    FROM ( ";
            sql += " SELECT VM.VIN_NO AS BODY_NO, VM.CHASSIS_NO, VM.[ORDER] AS PROD_ORDER, VM.SEQ_IN_LOT AS PROD_SEQ,  ";
            sql += " VM.MATL_GROUP_DESC AS MSC, ";
            sql += " mm.MODEL_ID AS MODEL,  VM.MODEL_NO AS MATL_GROUP,  ";
            sql += " VM.WBS_ELEM AS LOT,  ";
            sql += " VM.COLOR_CODE AS COLOR, ";
            sql += " (SELECT AG_INTERIOR_COLOR FROM QRINFO WHERE VM.VIN_NO = VIN_NO) AS AG_INTERIOR_COLOR, ";
            sql += " VM.COLOR_DESC AS AG_EXTERIOR_COLOR,  ";
            sql += " DATEDIFF(MINUTE, VM.UPDATE_DATETIME, GETDATE()) AS AGING,  ";
            sql += " (SELECT DISTINCT STATION_NAME FROM STATIONMST WHERE VM.OPERATION_ID = [STATION_ID]) AS STATION,   ";
            sql += @"  (CASE
                            WHEN
                              GETDATE() > vm.CASCADE_DATETIME AND
                              DATEDIFF(MINUTE, vm.CASCADE_DATETIME, GETDATE()) > 5 THEN 'Y'
                            ELSE 'N'
                          END) AS 'F2_CASCADE',";
            sql += @" CASE
                            WHEN EXISTS (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST sm WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                              AND (SELECT TOP 1 rt1.ACTION FROM REWORK_TIME rt1
                              WHERE rt1.VIN_NO = vm.VIN_NO ORDER BY rt1.DATE_TIME DESC) <> 'TakeIn'
                              AND vm.TAKEOUT_STATION IS NOT NULL
                              AND sm.STATION_NAME IS NOT NULL) THEN (
	                          SELECT DISTINCT sm.STATION_NAME
                              FROM (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST) sm
                              WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                              AND (SELECT TOP 1 rt1.ACTION
                              FROM REWORK_TIME rt1
                              WHERE rt1.VIN_NO = vm.VIN_NO
                              ORDER BY rt1.DATE_TIME DESC)
                              <> 'TakeIn'
                              AND vm.TAKEOUT_STATION IS NOT NULL
                              AND sm.STATION_NAME IS NOT NULL)
	                          ELSE '-'
                          END AS REWORK,";
            sql += " CONVERT(VARCHAR(24),VM.UPDATE_DATETIME,120) AS MATCH_DATE_TIME, ";
            sql += " VM.OPERATION_ID AS STATION_ID, vm.OPERATION_ACTION,  ";
            sql += " (SELECT TOP 1 DATETIME FROM SAPACTIONHISTORY WHERE CHASSIS_NO = vm.CHASSIS_NO AND OPERATION_ACTION = vm.OPERATION_ACTION ORDER BY DATETIME DESC) AS SAP_ACTION_DATETIME, ";
            sql += " VM.ENGINE_NO ";

            sql += " FROM VEHICLEMST VM, MODELMST mm ";
            sql += " WHERE VM.VIN_NO = VM.VIN_NO ";
            if (Lot != "ALL" && Lot != "")
                sql += " AND VM.WBS_ELEM =" + db.GetStr(Lot);
            if (Model != "ALL" && Model != "")
                sql += " AND VM.MODEL_NO =" + db.GetStr(Model);
            if (Color != "ALL" && Color != "")
                sql += " AND VM.COLOR_CODE =" + db.GetStr(Color);
            if (BasicCode != "ALL" && BasicCode != "")
                sql += " AND VM.MATL_GROUP_DESC =" + db.GetStr(BasicCode);
            if (ProdOrder != "ALL" && ProdOrder != "")
                sql += " AND VM.[ORDER] IN ( " + ProdOrder + ") ";
            if (FromStation != "")
            {
                sql += " AND VM.OPERATION_ID IN ";
                sql += " ( ";
                sql += "    SELECT DISTINCT sm.STATION_ID  ";
                sql += "    FROM STATIONMST sm ";
                sql += "    WHERE sm.STATION_ID IN ( ";
                sql += "               SELECT DISTINCT mm2.STATION_ID ";
                sql += "               FROM STATIONMST mm2 ";
                sql += "               WHERE mm2.STATION_ID IN ( " + FromStation + ")";// AND " + db.GetStr(ToStation);
                sql += "               AND (mm2.BROADCAST_FLAG IS NULL OR mm2.BROADCAST_FLAG = 'N') ";

                if (lineId != "")
                {
                    sql += " AND mm2.STATION_GROUP IN (" + lineId + ") "; //sql += " AND mm2.STATION_GROUP = " + db.GetStr(lineId); (10/6/19)
                }

                sql += "            )";
                sql += " ) ";
            }
            if (OpAction != "ALL" && OpAction != "")
                sql += " AND VM.OPERATION_ACTION IN ( " + OpAction + " ) ";//sql += " AND VM.OPERATION_ACTION = " + db.GetStr(OpAction);
            sql += " AND vm.MODEL_NO = mm.MATL_GROUP ";
            sql += " AND VM.CHASSIS_NO LIKE " + db.GetStr(chassisno);
            sql += " AND VM.ENGINE_NO LIKE " + db.GetStr(engineNo);

            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") "; //sql += " AND mm.LINE = " + db.GetStr(lineId); (10/6/19)
            }

            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }
            sql += " )allData WHERE 1=1 ";
            sql += " AND allData.OPERATION_ACTION NOT IN ('A070', 'A080') AND allData.CHASSIS_NO NOT IN (SELECT DISTINCT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (rework != "All" && rework != "")
                sql += " AND allData.REWORK IN ( " + rework + " ) ";

            sql += " ORDER BY allData.OPERATION_ACTION DESC, allData.SAP_ACTION_DATETIME DESC, allData.MATCH_DATE_TIME DESC  ";

            //db.GetDataTable(sql, out dt);
            dt = retrieveData(sql, hashtable);
            return dt;
        }

        public DataTable getViewAllWipInfo(DateTime from, string Lot, string Model, string Color, string BasicCode, string ProdOrder, string FromStation, string chassisno, string OpAction, string lineId, string Shift, string engineNo, string rework) //Bo Jun add OpAction
        {
            List<TimeSpan> ShiftTime = GetShiftTime(Shift);
            Hashtable hashtable = new Hashtable();

            string prod = ConfigurationManager.AppSettings["Production"].ToString();

            DataTable dt = null;

            string sql = "";

            sql += @" SELECT
                        allData.BODY_NO,
                        allData.CHASSIS_NO,
                        allData.PROD_ORDER,
                        allData.PROD_SEQ,
                        allData.MSC,
                        allData.MODEL,
                        allData.MATL_GROUP,
                        allData.ENGINE_NO,
                        allData.LOT,
                        allData.COLOR,
                        allData.DESTINATION,
                        allData.AG_INTERIOR_COLOR,
                        allData.AG_EXTERIOR_COLOR,
                        --allData.AGING,
                        allData.Station,
                        allData.F2_CASCADE,
                        allData.REWORK,
                        allData.MATCH_DATE_TIME AS SCANNED_DATE_TIME,
                        allData.STATION_ID,
                        allData.OPERATION_ACTION,
                        allData.SAP_ACTION_DATETIME,
                        CASE WHEN(AG.CHASSIS_NO IS NOT NULL) THEN 'Y' ELSE 'N' END AS BLOCK_FLAG,
                        allData.LINEOFF_DATETIME
                    FROM ( 
                    SELECT
	                    qr_data.VIN_NO AS BODY_NO,
	                    '-' AS CHASSIS_NO,
	                    '-' AS PROD_ORDER,
	                    0 AS PROD_SEQ,
	                    qr_data.MATERIAL_GROUP_DESC AS MSC,
	                    qr_data.MODEL_ID AS MODEL,
	                    qr_data.MATERIAL_GROUP AS MATL_GROUP,
	                    qr_data.LOT_NO AS LOT,
	                    '-' AS COLOR,
                        '-' AS DESTINATION,
	                    qr_data.AG_INTERIOR_COLOR,
	                    qr_data.AG_EXTERIOR_COLOR,
	                    DATEDIFF(MINUTE, qr_data.UPDATE_DATETIME, GETDATE()) AS AGING,
	                    sm.STATION_NAME AS Station,
	                    NULL AS LINEOFF_DATETIME,
	                    '-' AS F2_CASCADE,
	                    '-' AS REWORK,
	                    qr_data.UPDATE_DATETIME AS MATCH_DATE_TIME,
	                    sm.STATION_ID,
	                    '-' AS OPERATION_ACTION,
	                    NULL AS SAP_ACTION_DATETIME,
	                    '-' AS ENGINE_NO
                    FROM
                    (
	                    SELECT
		                    qr.*,
		                    'Q01' AS STATION_ID
	                    FROM
	                    (
		                    SELECT qr.*
		                    FROM VIEW_ALL_QRINFO qr
		                    LEFT JOIN
		                    (
                                SELECT DISTINCT vh.VIN_NO FROM
                                    (SELECT VIN_NO, COUNT(VIN_NO) AS [COUNT] FROM VIEW_ALL_VEHICLE_HISTORY GROUP BY VIN_NO) vh
                                WHERE vh.COUNT = 1
                            ) vh
		                    ON qr.VIN_NO = vh.VIN_NO
		                    WHERE qr.PRINT_DATETIME IS NOT NULL
		                    AND qr.VIN_NO IS NOT NULL
		                    AND qr.VIN_NO != ''
		                    AND qr.VIN_NO = vh.VIN_NO
		                    )qr )qr_data
                    LEFT JOIN
	                    (
	                    SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST
	                    ) sm
                    ON qr_data.STATION_ID = sm.STATION_ID
                    LEFT JOIN VIEW_ALL_VEHICLEMST vm
					ON vm.VIN_NO = qr_data.VIN_NO
                    WHERE qr_data.STATION_ID = 'Q01'";
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE " + db.GetStr(chassisno);
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE " + db.GetStr(engineNo);
            }

            sql += " UNION ";
            sql += " SELECT VM.VIN_NO AS BODY_NO, VM.CHASSIS_NO, VM.[ORDER] AS PROD_ORDER, VM.SEQ_IN_LOT AS PROD_SEQ, ";
            sql += " VM.MATL_GROUP_DESC AS MSC, ";
            sql += " mm.MODEL_ID AS MODEL,  VM.MODEL_NO AS MATL_GROUP,  ";
            //sql += " VM.MODEL_NAME AS SERIES,  ";
            sql += " VM.WBS_ELEM AS LOT,  ";
            sql += " VM.COLOR_CODE AS COLOR, VM.DESTINATION, ";
            sql += " (SELECT AG_INTERIOR_COLOR FROM VIEW_ALL_QRINFO WHERE VM.VIN_NO = VIN_NO) AS AG_INTERIOR_COLOR, ";
            sql += " VM.COLOR_DESC AS AG_EXTERIOR_COLOR,  ";
            //sql += " CASE WHEN VM.ENGINE_NO = '' OR VM.ENGINE_NO IS NULL THEN '-' ELSE VM.ENGINE_NO END AS ENGINE_NO,  ";
            sql += " DATEDIFF(MINUTE, VM.UPDATE_DATETIME, GETDATE()) AS AGING,  ";
            sql += " (SELECT DISTINCT STATION_NAME FROM STATIONMST WHERE VM.OPERATION_ID = [STATION_ID]) AS STATION,   ";
            sql += " (SELECT UPDATE_DATETIME FROM VIEW_ALL_VEHICLE_HISTORY WHERE VIN_NO = VM.VIN_NO AND STATION_ID = (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE LINE_DESC = 'LINEOFF')) AS LINEOFF_DATETIME,   ";
            sql += @"  (CASE
                            WHEN
                              GETDATE() > vm.CASCADE_DATETIME AND
                              DATEDIFF(MINUTE, vm.CASCADE_DATETIME, GETDATE()) > 5 THEN 'Y'
                            ELSE 'N'
                          END) AS 'F2_CASCADE',";
            sql += @" CASE
                            WHEN EXISTS (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST sm WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                              AND (SELECT TOP 1 rt1.ACTION FROM VIEW_ALL_REWORK_TIME rt1
                              WHERE rt1.VIN_NO = vm.VIN_NO ORDER BY rt1.DATE_TIME DESC) <> 'TakeIn'
                              AND vm.TAKEOUT_STATION IS NOT NULL
                              AND sm.STATION_NAME IS NOT NULL) THEN (
	                          SELECT DISTINCT sm.STATION_NAME
                              FROM (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST) sm
                              WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                              AND (SELECT TOP 1 rt1.ACTION
                              FROM VIEW_ALL_REWORK_TIME rt1
                              WHERE rt1.VIN_NO = vm.VIN_NO
                              ORDER BY rt1.DATE_TIME DESC)
                              <> 'TakeIn'
                              AND vm.TAKEOUT_STATION IS NOT NULL
                              AND sm.STATION_NAME IS NOT NULL)
	                          ELSE '-'
                          END AS REWORK,";
            sql += " CONVERT(VARCHAR(24),VM.UPDATE_DATETIME,120) AS MATCH_DATE_TIME, ";
            sql += " VM.OPERATION_ID AS STATION_ID, vm.OPERATION_ACTION,  ";
            sql += " (SELECT TOP 1 DATETIME FROM SAPACTIONHISTORY WHERE CHASSIS_NO = vm.CHASSIS_NO AND OPERATION_ACTION = vm.OPERATION_ACTION ORDER BY DATETIME DESC) AS SAP_ACTION_DATETIME, ";
            sql += " VM.ENGINE_NO ";
            //sql += " VM.UPDATE_DATETIME AS DATE_TIME ";

            sql += " FROM VIEW_ALL_VEHICLEMST VM, MODELMST mm ";
            sql += " WHERE VM.VIN_NO = VM.VIN_NO ";
            sql += " AND VM.UPDATE_DATETIME <= " + db.GetDateTime(from);// +" AND " + db.GetDateTime(to); //Comment by KCC 20170222

            #region Old Code

            /*if (Lot != "ALL" && Lot != "")
                sql += " AND VM.WBS_ELEM IN ( " + Lot + " ) ";//sql += " AND VM.WBS_ELEM =" + db.GetStr(Lot);
            if (Model != "ALL" && Model != "")
                sql += " AND VM.MODEL_NO IN ( " + Model + " ) ";//sql += " AND VM.MODEL_NO =" + db.GetStr(Model);
            if (Color != "ALL" && Color != "")
                sql += " AND VM.COLOR_CODE IN ( " + Color + " ) ";//sql += " AND VM.COLOR_CODE =" + db.GetStr(Color);
            if (BasicCode != "ALL" && BasicCode != "")
                sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";//sql += " AND VM.MATL_GROUP_DESC =" + db.GetStr(BasicCode);
            if (ProdOrder != "ALL" && ProdOrder != "")
                sql += " AND VM.[ORDER] IN ( " + ProdOrder + " ) "; //sql += " AND VM.[ORDER] LIKE "+ db.GetStr(ProdOrder) (10/6/19);
            if (FromStation != "")
            {
                //sql += " AND VH.STATION_ID BETWEEN " + db.GetStr(FromStation) + " AND " + db.GetStr(ToStation);
                sql += " AND VM.OPERATION_ID IN ";
                sql += " ( ";
                sql += "    SELECT DISTINCT sm.STATION_ID  ";
                sql += "    FROM STATIONMST sm ";
                //sql += "    WHERE sm.SORTING_SEQ BETWEEN " + db.GetStr(FromStation) + " AND " + db.GetStr(ToStation);
                sql += "    WHERE sm.STATION_ID IN ( ";
                sql += "               SELECT DISTINCT mm2.STATION_ID ";
                sql += "               FROM STATIONMST mm2 ";
                sql += "               WHERE mm2.STATION_ID IN (" + FromStation + ")";// + " AND " + db.GetStr(ToStation);
                sql += "               AND (mm2.BROADCAST_FLAG IS NULL OR mm2.BROADCAST_FLAG = 'N') ";

                if (lineId != "")
                {
                    sql += " AND mm2.STATION_GROUP IN (" + lineId + ") "; //sql += " AND mm2.STATION_GROUP = " + db.GetStr(lineId); (10/6/19)
                }

                sql += "            )";
                sql += " ) ";
            }
            if (OpAction != "ALL" && OpAction != "")
                sql += " AND VM.OPERATION_ACTION IN ( " + OpAction + " ) ";//sql += " AND VM.OPERATION_ACTION = " + db.GetStr(OpAction);
                                                                           //if (chassisno != "ALL") //Comment by KCC 20170221*/

            #endregion Old Code

            sql += " AND vm.MODEL_NO = mm.MATL_GROUP ";
            sql += " AND VM.CHASSIS_NO LIKE " + db.GetStr(chassisno);
            sql += " AND VM.ENGINE_NO LIKE " + db.GetStr(engineNo);

            /* if (lineId != "")
             {
                 sql += " AND mm.LINE IN (" + lineId + ") "; //sql += " AND mm.LINE = " + db.GetStr(lineId); (10/6/19)
             }*/

            sql += @" 
            ) allData
            LEFT JOIN MODELMST mm
            ON mm.MODEL_ID = allData.MODEL
            AND mm.MATL_GROUP = allData.MATL_GROUP ";

            if (prod != "Y")
            {
                sql += @"
                    LEFT JOIN (SELECT DISTINCT CHASSIS_NO FROM AG_BLOCK) AG
                    ON allData.CHASSIS_NO = AG.CHASSIS_NO";
            }

            sql += " WHERE allData.OPERATION_ACTION NOT IN ('A070', 'A080') AND allData.CHASSIS_NO NOT IN (SELECT DISTINCT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (rework != "All" && rework != "")
                sql += " AND allData.REWORK IN ( " + rework + " ) ";
            if (FromStation != "")
            {
                sql += " AND allData.STATION_ID IN ";
                sql += " ( ";
                sql += "    SELECT DISTINCT sm.STATION_ID  ";
                sql += "    FROM STATIONMST sm ";
                sql += "    WHERE sm.STATION_ID IN ( ";
                sql += "               SELECT DISTINCT mm2.STATION_ID ";
                sql += "               FROM STATIONMST mm2 ";
                sql += "               WHERE mm2.STATION_ID IN (" + FromStation + ")";
                sql += "               AND (mm2.BROADCAST_FLAG IS NULL OR mm2.BROADCAST_FLAG = 'N') ";

                if (lineId != "")
                {
                    sql += " AND mm2.STATION_GROUP IN (" + lineId + ") ";
                }

                sql += "            )";
                sql += " ) ";
            }
            if (Lot != "ALL" && Lot != "")
                sql += " AND allData.LOT IN ( " + Lot + " ) ";
            if (Model != "ALL" && Model != "")
                sql += " AND allData.MATL_GROUP IN ( " + Model + " ) ";
            if (Color != "ALL" && Color != "")
                sql += " AND allData.COLOR IN ( " + Color + " ) ";
            if (BasicCode != "ALL" && BasicCode != "")
                sql += " AND allData.MSC IN ( " + BasicCode + " ) ";
            if (ProdOrder != "ALL" && ProdOrder != "")
                sql += " AND allData.PROD_ORDER IN ( " + ProdOrder + " ) ";
            if (OpAction != "ALL" && OpAction != "")
                sql += " AND allData.OPERATION_ACTION IN ( " + OpAction + " ) ";
            if (lineId != "")
                sql += " AND mm.LINE IN (" + lineId + ") ";
            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),allData.MATCH_DATE_TIME) >= @START_TIME
                        AND CONVERT(time(7),allData.MATCH_DATE_TIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),allData.MATCH_DATE_TIME) >= @START_TIME AND CONVERT(time(7),allData.MATCH_DATE_TIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),allData.MATCH_DATE_TIME) >= '00:00:00.0000000' AND CONVERT(time(7),allData.MATCH_DATE_TIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += " ORDER BY allData.OPERATION_ACTION DESC, allData.SAP_ACTION_DATETIME DESC, allData.MATCH_DATE_TIME DESC  ";

            //db.GetDataTable(sql, out dt);
            dt = retrieveData(sql, hashtable);
            return dt;
        }

        public DataTable getViewAllWipInfoNoDate(string Lot, string Model, string Color, string BasicCode, string ProdOrder, string FromStation, string chassisno, string OpAction, string lineId, string Shift, string engineNo, string rework) //Bo Jun add OpAction
        {
            List<TimeSpan> ShiftTime = GetShiftTime(Shift);
            Hashtable hashtable = new Hashtable();

            string prod = ConfigurationManager.AppSettings["Production"].ToString();

            DataTable dt = null;

            string sql = "";

            sql += @" SELECT
                        allData.BODY_NO,
                        allData.CHASSIS_NO,
                        allData.PROD_ORDER,
                        allData.PROD_SEQ,
                        allData.MSC,
                        allData.MODEL,
                        allData.MATL_GROUP,
                        allData.ENGINE_NO,
                        allData.LOT,
                        allData.COLOR,
                        allData.DESTINATION,
                        allData.AG_INTERIOR_COLOR,
                        allData.AG_EXTERIOR_COLOR,
                        --allData.AGING,
                        allData.Station,
                        allData.F2_CASCADE,
                        allData.REWORK,
                        allData.MATCH_DATE_TIME AS SCANNED_DATE_TIME,
                        allData.STATION_ID,
                        allData.OPERATION_ACTION,
                        allData.SAP_ACTION_DATETIME,
                        CASE WHEN(AG.CHASSIS_NO IS NOT NULL) THEN 'Y' ELSE 'N' END AS BLOCK_FLAG,
                        allData.LINEOFF_DATETIME 
            ";

            sql += " FROM ( SELECT VM.VIN_NO AS BODY_NO, VM.CHASSIS_NO, VM.[ORDER] AS PROD_ORDER, VM.SEQ_IN_LOT AS PROD_SEQ, ";
            sql += " VM.MATL_GROUP_DESC AS MSC, ";
            sql += " mm.MODEL_ID AS MODEL,  VM.MODEL_NO AS MATL_GROUP,  ";
            //sql += " VM.MODEL_NAME AS SERIES,  ";
            sql += " VM.WBS_ELEM AS LOT,  ";
            sql += " VM.COLOR_CODE AS COLOR,  VM.DESTINATION, ";
            sql += " (SELECT AG_INTERIOR_COLOR FROM VIEW_ALL_QRINFO WHERE VM.VIN_NO = VIN_NO) AS AG_INTERIOR_COLOR, ";
            sql += " VM.COLOR_DESC AS AG_EXTERIOR_COLOR,  ";
            //sql += " CASE WHEN VM.ENGINE_NO = '' OR VM.ENGINE_NO IS NULL THEN '-' ELSE VM.ENGINE_NO END AS ENGINE_NO,  ";
            sql += " DATEDIFF(MINUTE, VM.UPDATE_DATETIME, GETDATE()) AS AGING,  ";
            sql += " (SELECT DISTINCT STATION_NAME FROM STATIONMST WHERE VM.OPERATION_ID = [STATION_ID]) AS STATION,   ";
            sql += " (SELECT UPDATE_DATETIME FROM VIEW_ALL_VEHICLE_HISTORY WHERE VIN_NO = VM.VIN_NO AND STATION_ID = (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE LINE_DESC = 'LINEOFF')) AS LINEOFF_DATETIME,   ";
            sql += @"  (CASE
                            WHEN
                              GETDATE() > vm.CASCADE_DATETIME AND
                              DATEDIFF(MINUTE, vm.CASCADE_DATETIME, GETDATE()) > 5 THEN 'Y'
                            ELSE 'N'
                          END) AS 'F2_CASCADE',";
            sql += @" CASE
                            WHEN EXISTS (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST sm WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                              AND (SELECT TOP 1 rt1.ACTION FROM VIEW_ALL_REWORK_TIME rt1
                              WHERE rt1.VIN_NO = vm.VIN_NO ORDER BY rt1.DATE_TIME DESC) <> 'TakeIn'
                              AND vm.TAKEOUT_STATION IS NOT NULL
                              AND sm.STATION_NAME IS NOT NULL) THEN (
	                          SELECT DISTINCT sm.STATION_NAME
                              FROM (SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST) sm
                              WHERE sm.STATION_ID = vm.TAKEOUT_STATION
                              AND (SELECT TOP 1 rt1.ACTION
                              FROM VIEW_ALL_REWORK_TIME rt1
                              WHERE rt1.VIN_NO = vm.VIN_NO
                              ORDER BY rt1.DATE_TIME DESC)
                              <> 'TakeIn'
                              AND vm.TAKEOUT_STATION IS NOT NULL
                              AND sm.STATION_NAME IS NOT NULL)
	                          ELSE '-'
                          END AS REWORK,";
            sql += " CONVERT(VARCHAR(24),VM.UPDATE_DATETIME,120) AS MATCH_DATE_TIME, ";
            sql += " VM.OPERATION_ID AS STATION_ID, vm.OPERATION_ACTION,  ";
            sql += " (SELECT TOP 1 DATETIME FROM VIEW_ALL_SAPACTIONHISTORY WHERE CHASSIS_NO = vm.CHASSIS_NO AND OPERATION_ACTION = vm.OPERATION_ACTION ORDER BY DATETIME DESC) AS SAP_ACTION_DATETIME, ";
            sql += " VM.ENGINE_NO ";
            //sql += " VM.UPDATE_DATETIME AS DATE_TIME ";

            sql += " FROM VIEW_ALL_VEHICLEMST VM, MODELMST mm ";
            sql += " WHERE VM.VIN_NO = VM.VIN_NO ";
            //sql += " AND VM.UPDATE_DATETIME <= " + db.GetDateTime(from);// +" AND " + db.GetDateTime(to); //Comment by KCC 20170222
            if (Lot != "ALL" && Lot != "")
                sql += " AND VM.WBS_ELEM IN ( " + Lot + " ) ";//sql += " AND VM.WBS_ELEM =" + db.GetStr(Lot);
            if (Model != "ALL" && Model != "")
                sql += " AND VM.MODEL_NO IN ( " + Model + " ) ";//sql += " AND VM.MODEL_NO =" + db.GetStr(Model);
            if (Color != "ALL" && Color != "")
                sql += " AND VM.COLOR_CODE IN ( " + Color + " ) ";//sql += " AND VM.COLOR_CODE =" + db.GetStr(Color);
            if (BasicCode != "ALL" && BasicCode != "")
                sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";//sql += " AND VM.MATL_GROUP_DESC =" + db.GetStr(BasicCode);
            if (ProdOrder != "ALL" && ProdOrder != "")
                sql += " AND VM.[ORDER] IN ( " + ProdOrder + ") ";
            if (FromStation != "")
            {
                //sql += " AND VH.STATION_ID BETWEEN " + db.GetStr(FromStation) + " AND " + db.GetStr(ToStation);
                sql += " AND VM.OPERATION_ID IN ";
                sql += " ( ";
                sql += "    SELECT DISTINCT sm.STATION_ID  ";
                sql += "    FROM STATIONMST sm ";
                //sql += "    WHERE sm.SORTING_SEQ BETWEEN " + db.GetStr(FromStation) + " AND " + db.GetStr(ToStation);
                sql += "    WHERE sm.STATION_ID IN ( ";
                sql += "               SELECT DISTINCT mm2.STATION_ID ";
                sql += "               FROM STATIONMST mm2 ";
                sql += "               WHERE mm2.STATION_ID IN ( " + FromStation + ")";// + " AND " + db.GetStr(ToStation);
                sql += "               AND (mm2.BROADCAST_FLAG IS NULL OR mm2.BROADCAST_FLAG = 'N') ";

                if (lineId != "")
                {
                    sql += "               AND mm2.STATION_GROUP IN (" + lineId + ") ";
                }

                sql += "            )";
                sql += " ) ";
            }
            if (OpAction != "ALL" && OpAction != "")
                sql += " AND VM.OPERATION_ACTION IN ( " + OpAction + " ) ";//sql += " AND VM.OPERATION_ACTION = " + db.GetStr(OpAction);
            //if (chassisno != "ALL") //Comment by KCC 20170221
            sql += " AND vm.MODEL_NO = mm.MATL_GROUP ";
            sql += " AND VM.CHASSIS_NO LIKE " + db.GetStr(chassisno);
            sql += " AND VM.ENGINE_NO LIKE " + db.GetStr(engineNo);

            if (lineId != "")
            {
                sql += " AND mm2.STATION_GROUP IN (" + lineId + ") ";
            }

            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += @" 
            ) allData ";

            sql += @"
            LEFT JOIN (SELECT DISTINCT CHASSIS_NO FROM AG_BLOCK) AG
            ON allData.CHASSIS_NO = AG.CHASSIS_NO
            WHERE 1=1 
            AND allData.OPERATION_ACTION NOT IN ('A070', 'A080') AND allData.CHASSIS_NO NOT IN (SELECT DISTINCT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (rework != "All" && rework != "")
                sql += " AND allData.REWORK IN ( " + rework + " ) ";

            sql += " ORDER BY allData.OPERATION_ACTION DESC, allData.SAP_ACTION_DATETIME DESC, allData.MATCH_DATE_TIME DESC  ";

            //db.GetDataTable(sql, out dt);
            dt = retrieveData(sql, hashtable);
            return dt;
        }

        public DataTable getAllWIPCount(string Lot, string Model, string Color, string BasicCode, string ProdOrder, string station, string chassisno, string OpAction, string lineId, string Shift, string engineNo, string rework)
        {
            List<TimeSpan> ShiftTime = GetShiftTime(Shift);
            Hashtable hashtable = new Hashtable();
            string sql =
                @"
                    SELECT
                      STATION_ID, STATION_NAME, DISPLAY_SEQ, IN_LINE, F1_REWORK, F2_REWORK, TOTAL_WIP, F2_CASCADE
                    FROM ((SELECT
                      sm.STATION_ID,
                      sm.STATION_NAME,
                      sm.DISPLAY_SEQ,
                      COUNT(vh.VIN_NO) AS [IN_LINE],
                      0 AS [F1_REWORK],
                      0 AS [F2_REWORK],
                      0 AS [F2_CASCADE],
                      (COUNT(vh.VIN_NO)) AS [TOTAL_WIP]
                    FROM QRINFO QR
                    LEFT JOIN (
                        SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
                        WHERE STATUS = 'Y'
                        AND (BROADCAST_FLAG <> 'Y' OR BROADCAST_FLAG IS NULL)
                    ) sm
                    ON sm.STATION_ID = 'Q01'
                    LEFT JOIN VEHICLEMST VM
                    ON VM.VIN_NO = QR.VIN_NO
                    LEFT JOIN (
                        SELECT DISTINCT vh.VIN_NO FROM
                            (SELECT VIN_NO, COUNT(VIN_NO) AS [COUNT] FROM VEHICLE_HISTORY GROUP BY VIN_NO) vh
                        WHERE vh.COUNT = 1
                    ) vh
                    ON VH.VIN_NO = QR.VIN_NO
                    LEFT JOIN MODELMST mm
                    ON mm.MATL_GROUP = qr.MATERIAL_GROUP
					AND mm.MODEL_ID = qr.MODEL_ID
                    WHERE 1 = 1
                    AND vh.VIN_NO IS NOT NULL
                    AND ((SELECT TOP 1
                      rt1.ACTION
                    FROM REWORK_TIME rt1
                    WHERE rt1.VIN_NO = vH.VIN_NO
                    ORDER BY rt1.DATE_TIME DESC)
                    = 'TakeIn'
                    OR VH.VIN_NO IS NOT NULL)";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND qr.MATERIAL_GROUP IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND qr.MATERIAL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (OpAction != "ALL" && OpAction != "")
            {
                sql += " AND vm.OPERATION_ACTION IN ( " + OpAction + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }

            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            if (rework != "All" && rework != "")
                sql += " AND vm.TAKEOUT_STATION IN (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE STATION_NAME = " + rework + " ) ";

            sql += @"  GROUP BY SM.STATION_ID,
                             SM.STATION_NAME,
                             SM.DISPLAY_SEQ)

                    UNION

                    (SELECT DISTINCT
                      allData.STATION_ID,
                      allData.STATION_NAME,
                      allData.DISPLAY_SEQ,
                      (SUM(allData.IN_LINE) OVER (PARTITION BY allData.STATION_ID) - ISNULL(cas_data.CAS_COUNT, 0)) AS IN_LINE,";

            if (rework != "All" && rework != "")
            {
                if (rework == "'F1 Rework'")
                {
                    sql += @"
                        SUM(allData.F1_REWORK) OVER(PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(ISNULL(allData.F2_REWORK, 0)) OVER(PARTITION BY allData.STATION_ID) AS F2_REWORK,";
                }
                else if (rework == "'F2 Rework'")
                {
                    sql += @"
                        SUM(ISNULL(allData.F1_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(allData.F2_REWORK) OVER (PARTITION BY allData.STATION_ID) AS F2_REWORK,";
                }
                else
                {
                    sql += @"
                        SUM(ISNULL(allData.F1_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(ISNULL(allData.F2_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F2_REWORK,";
                }
            }
            else
            {
                sql += @"
                        SUM(ISNULL(allData.F1_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(ISNULL(allData.F2_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F2_REWORK,";
            }

            sql += @"
                      ISNULL(cas_data.CAS_COUNT, 0) AS [F2_CASCADE],
                      (SUM(allData.[COUNT]) OVER (PARTITION BY allData.STATION_ID) - ISNULL(cas_data.CAS_COUNT, 0)) AS [TOTAL_WIP]
                    FROM ((
                        SELECT
					    sm.STATION_ID, sm.STATION_NAME, sm.DISPLAY_SEQ,
					    CASE WHEN (in_line.IN_LINE IS NULL) THEN  0 ELSE in_line.IN_LINE END [IN_LINE],
                        NULL AS [F1_REWORK],
                        NULL AS [F2_REWORK],
                        NULL AS [F2_CASCADE],
                        CASE WHEN (in_line.IN_LINE IS NULL) THEN  0 ELSE in_line.IN_LINE END AS [COUNT]
					    FROM
					    (
                            SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
                            WHERE STATUS = 'Y'
                            AND (BROADCAST_FLAG <> 'Y'
                            OR BROADCAST_FLAG IS NULL)
                            AND (LINE_DESC <> 'Rework'
                            OR LINE_DESC IS NULL)
                            AND STATION_ID NOT IN ('Q01')
                        ) sm
					    LEFT JOIN
					    (
                            SELECT
                              sm.STATION_ID,
                              sm.STATION_NAME,
                              sm.DISPLAY_SEQ,
                              COUNT(vm.VIN_NO) AS [IN_LINE],
                              NULL AS [F1_REWORK],
                              NULL AS [F2_REWORK],
                              NULL AS [F2_CASCADE],
                              COUNT(vm.VIN_NO) AS [COUNT]
                            FROM (
                                SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
                                WHERE STATUS = 'Y'
                                AND (BROADCAST_FLAG <> 'Y'
                                OR BROADCAST_FLAG IS NULL)
                                AND (LINE_DESC <> 'Rework'
                                OR LINE_DESC IS NULL)
                                AND STATION_ID NOT IN ('Q01')
                            ) sm
                            LEFT JOIN VEHICLEMST vm
                            ON sm.STATION_ID = vm.OPERATION_ID
                            LEFT JOIN MODELMST mm
                            ON mm.MATL_GROUP = vm.MODEL_NO
                            WHERE ((SELECT TOP 1
                              rt.ACTION
                            FROM REWORK_TIME rt
                            WHERE rt.VIN_NO = vm.VIN_NO
                            ORDER BY rt.DATE_TIME DESC)
                            = 'TakeIn'
                            OR vm.TAKEOUT_STATION IS NULL)
                            AND sm.STATION_NAME IS NOT NULL
                            AND (vm.OPERATION_ACTION NOT IN ('A070', 'A080'))
                            AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND vm.MODEL_NO IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND vm.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (ProdOrder != "ALL" && ProdOrder != "")
            {
                sql += " AND vm.[ORDER] IN ( " + ProdOrder + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += @"
                    GROUP BY vm.OPERATION_ID,
                    sm.STATION_ID,
                    sm.STATION_NAME,
                    sm.DISPLAY_SEQ) in_line
					ON sm.STATION_ID = in_line.STATION_ID

                    UNION

                    SELECT DISTINCT
                      rework_data.STATION_ID,
                      rework_data.STATION_NAME,
                      rework_data.DISPLAY_SEQ,
                      rework_data.[IN_LINE],
                      SUM(rework_data.F1_REWORK) OVER (PARTITION BY rework_data.STATION_ID) AS [F1_REWORK],
                      SUM(rework_data.F2_REWORK) OVER (PARTITION BY rework_data.STATION_ID) AS [F2_REWORK],
                      rework_data.[F2_CASCADE],
                      SUM(rework_data.[COUNT]) OVER (PARTITION BY rework_data.STATION_ID) AS [TOTAL_WIP]
                    FROM (SELECT
                      sm.STATION_ID,
                      sm.STATION_NAME,
                      sm.DISPLAY_SEQ,
                      0 AS [IN_LINE],
                      CASE
                        WHEN (vm.TAKEOUT_STATION = 'Q05') THEN COUNT(vm.VIN_NO)
                        ELSE NULL
                      END AS [F1_REWORK],
                      CASE
                        WHEN (vm.TAKEOUT_STATION = 'Q08') THEN COUNT(vm.VIN_NO)
                        ELSE NULL
                      END AS [F2_REWORK],
                      0 AS [F2_CASCADE],
                      COUNT(vm.VIN_NO) AS [COUNT]
                    FROM VEHICLEMST VM
                    LEFT JOIN (
                        SELECT DISTINCT
                          STATION_ID,
                          STATION_NAME,
                          DISPLAY_SEQ
                        FROM STATIONMST
                        WHERE STATUS = 'Y'
                        AND (BROADCAST_FLAG <> 'Y'
                        OR BROADCAST_FLAG IS NULL)
                        AND STATION_ID NOT IN ('Q01')
                    ) sm
                    ON sm.STATION_ID = vm.OPERATION_ID
                    LEFT JOIN MODELMST mm
                    ON mm.MATL_GROUP = vm.MODEL_NO
                    WHERE (SELECT TOP 1
                      rt.ACTION
                    FROM REWORK_TIME rt
                    WHERE rt.VIN_NO = vm.VIN_NO
                    ORDER BY rt.DATE_TIME DESC)
                    <> 'TakeIn'
                    AND vm.TAKEOUT_STATION IS NOT NULL
                    AND sm.STATION_NAME IS NOT NULL
                    AND (vm.OPERATION_ACTION NOT IN ('A070', 'A080')
                    OR vm.OPERATION_ACTION IS NULL)
                    AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND vm.MODEL_NO IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (ProdOrder != "ALL" && ProdOrder != "")
            {
                sql += " AND vm.[ORDER] IN ( " + ProdOrder + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += @"
                    GROUP BY sm.STATION_ID,
                    sm.STATION_NAME,
                    sm.DISPLAY_SEQ,
                    vm.TAKEOUT_STATION) rework_data
                    )) allData

                    LEFT JOIN (SELECT
                      COUNT(cas.VIN_NO) AS [CAS_COUNT],
                      cas.OPERATION_ID
                    FROM (SELECT DISTINCT
                      VM.VIN_NO,
                      VM.OPERATION_ID,
                      CASE
                        WHEN (GETDATE() > vm.CASCADE_DATETIME AND
                          DATEDIFF(MINUTE, vm.CASCADE_DATETIME, GETDATE()) > 5) THEN VM.CASCADE_DATETIME
                        ELSE NULL
                      END AS CASCADE_DATETIME
                    FROM VEHICLEMST VM
                    LEFT JOIN MODELMST mm
                    ON mm.MATL_GROUP = vm.MODEL_NO
                    WHERE VM.CASCADE_DATETIME IS NOT NULL
                    AND VM.OPERATION_ACTION NOT IN ('A070', 'A080')
                    AND VM.OPERATION_ID NOT IN ('Q10')
                    AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))
                    --(SELECT DISTINCT
                     -- STATION_ID
                    --FROM STATIONMST
                   -- WHERE MAIN_SUB = 'SUB')
                    ";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND vm.MODEL_NO IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (ProdOrder != "ALL" && ProdOrder != "")
            {
                sql += " AND vm.[ORDER] IN ( " + ProdOrder + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += @"
                    GROUP BY VM.OPERATION_ID,
                             VM.VIN_NO,
                             VM.CASCADE_DATETIME) cas
                    WHERE cas.CASCADE_DATETIME IS NOT NULL
                    GROUP BY cas.OPERATION_ID) cas_data
                      ON allData.STATION_ID = cas_data.OPERATION_ID

                    GROUP BY allData.STATION_ID,
                             allData.STATION_NAME,
                             allData.DISPLAY_SEQ,
                             allData.IN_LINE,
                             allData.F1_REWORK,
                             allData.F2_REWORK,
                             allData.F2_CASCADE,
                             allData.[COUNT],
                             cas_data.CAS_COUNT
                    )) wip_data
                    WHERE 1 = 1";

            if (rework != "All" && rework != "")
            {
                if (rework.Equals("'F1 Rework'"))
                {
                    sql += @" AND wip_data.[F1_REWORK] IS NOT NULL";
                }

                if (rework.Equals("'F2 Rework'"))
                {
                    sql += @" AND wip_data.[F2_REWORK] IS NOT NULL";
                }
            }

            if (!string.IsNullOrEmpty(station))
            {
                sql += @" AND wip_data.STATION_ID IN (" + station + ") ";
            }

            sql += @" ORDER BY DISPLAY_SEQ";

            return retrieveData(sql, hashtable);
        }

        public DataTable getAllViewWIPCount(string Lot, string Model, string Color, string BasicCode, string ProdOrder, string station, string chassisno, string OpAction, string lineId, string Shift, string engineNo, string rework)
        {
            List<TimeSpan> ShiftTime = GetShiftTime(Shift);
            Hashtable hashtable = new Hashtable();
            string sql =
                @"
                    SELECT
                      *
                    FROM ((SELECT
                      sm.STATION_ID,
                      sm.STATION_NAME,
                      sm.DISPLAY_SEQ,
                      COUNT(vh.VIN_NO) AS [IN_LINE],
                      0 AS [F1_REWORK],
                      0 AS [F2_REWORK],
                      0 AS [F2_CASCADE],
                      (COUNT(vh.VIN_NO)) AS [TOTAL_WIP]
                    FROM VIEW_ALL_QRINFO QR
                    LEFT JOIN (
                        SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
                        WHERE STATUS = 'Y'
                        AND (BROADCAST_FLAG <> 'Y' OR BROADCAST_FLAG IS NULL)
                    ) sm
                    ON sm.STATION_ID = 'Q01'
                    LEFT JOIN VIEW_ALL_VEHICLEMST VM
                    ON VM.VIN_NO = QR.VIN_NO
                    LEFT JOIN (
                        SELECT DISTINCT vh.VIN_NO FROM
                            (SELECT VIN_NO, COUNT(VIN_NO) AS [COUNT] FROM VIEW_ALL_VEHICLE_HISTORY GROUP BY VIN_NO) vh
                        WHERE vh.COUNT = 1
                    ) vh
                    ON VH.VIN_NO = QR.VIN_NO
                    LEFT JOIN MODELMST mm
                    ON mm.MATL_GROUP = vm.MODEL_NO
                    WHERE 1 = 1
                    AND vh.VIN_NO IS NOT NULL
                    AND ((SELECT TOP 1
                      rt1.ACTION
                    FROM VIEW_ALL_REWORK_TIME rt1
                    WHERE rt1.VIN_NO = vH.VIN_NO
                    ORDER BY rt1.DATE_TIME DESC)
                    = 'TakeIn'
                    OR VH.VIN_NO IS NOT NULL)";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND vm.MODEL_NO IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (OpAction != "ALL" && OpAction != "")
            {
                sql += " AND vm.OPERATION_ACTION IN ( " + OpAction + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }

            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            if (rework != "All" && rework != "")
                sql += " AND vm.TAKEOUT_STATION IN (SELECT DISTINCT STATION_ID FROM STATIONMST WHERE STATION_NAME = " + rework + " ) ";

            sql += @"  GROUP BY SM.STATION_ID,
                             SM.STATION_NAME,
                             SM.DISPLAY_SEQ)

                    UNION

                    (SELECT DISTINCT
                      allData.STATION_ID,
                      allData.STATION_NAME,
                      allData.DISPLAY_SEQ,
                      (SUM(allData.IN_LINE) OVER (PARTITION BY allData.STATION_ID) - ISNULL(cas_data.CAS_COUNT, 0)) AS IN_LINE,";

            if (rework != "All" && rework != "")
            {
                if (rework == "'F1 Rework'")
                {
                    sql += @"
                        SUM(allData.F1_REWORK) OVER(PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(ISNULL(allData.F2_REWORK, 0)) OVER(PARTITION BY allData.STATION_ID) AS F2_REWORK,";
                }
                else if (rework == "'F2 Rework'")
                {
                    sql += @"
                        SUM(ISNULL(allData.F1_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(allData.F2_REWORK) OVER (PARTITION BY allData.STATION_ID) AS F2_REWORK,";
                }
                else
                {
                    sql += @"
                        SUM(ISNULL(allData.F1_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(ISNULL(allData.F2_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F2_REWORK,";
                }
            }
            else
            {
                sql += @"
                        SUM(ISNULL(allData.F1_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F1_REWORK,
                        SUM(ISNULL(allData.F2_REWORK, 0)) OVER (PARTITION BY allData.STATION_ID) AS F2_REWORK,";
            }

            sql += @"
                      ISNULL(cas_data.CAS_COUNT, 0) AS [F2_CASCADE],
                      (SUM(allData.[COUNT]) OVER (PARTITION BY allData.STATION_ID) - ISNULL(cas_data.CAS_COUNT, 0)) AS [COUNT]
                    FROM ((
                    SELECT
					    sm.STATION_ID, sm.STATION_NAME, sm.DISPLAY_SEQ,
					    CASE WHEN (in_line.IN_LINE IS NULL) THEN  0 ELSE in_line.IN_LINE END [IN_LINE],
                        NULL AS [F1_REWORK],
                        NULL AS [F2_REWORK],
                        NULL AS [F2_CASCADE],
                        CASE WHEN (in_line.IN_LINE IS NULL) THEN  0 ELSE in_line.IN_LINE END AS [COUNT]
					    FROM
					    (
                            SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
                            WHERE STATUS = 'Y'
                            AND (BROADCAST_FLAG <> 'Y'
                            OR BROADCAST_FLAG IS NULL)
                            AND (LINE_DESC <> 'Rework'
                            OR LINE_DESC IS NULL)
                            AND STATION_ID NOT IN ('Q01')
                        ) sm
					    LEFT JOIN
					    (
                            SELECT
                              sm.STATION_ID,
                              sm.STATION_NAME,
                              sm.DISPLAY_SEQ,
                              COUNT(vm.VIN_NO) AS [IN_LINE],
                              NULL AS [F1_REWORK],
                              NULL AS [F2_REWORK],
                              NULL AS [F2_CASCADE],
                              COUNT(vm.VIN_NO) AS [COUNT]
                            FROM (
                                SELECT DISTINCT STATION_ID, STATION_NAME, DISPLAY_SEQ FROM STATIONMST
                                WHERE STATUS = 'Y'
                                AND (BROADCAST_FLAG <> 'Y'
                                OR BROADCAST_FLAG IS NULL)
                                AND (LINE_DESC <> 'Rework'
                                OR LINE_DESC IS NULL)
                                AND STATION_ID NOT IN ('Q01')
                            ) sm
                            LEFT JOIN VIEW_ALL_VEHICLEMST vm
                            ON sm.STATION_ID = vm.OPERATION_ID
                            LEFT JOIN MODELMST mm
                            ON mm.MATL_GROUP = vm.MODEL_NO
                            WHERE ((SELECT TOP 1
                              rt.ACTION
                            FROM VIEW_ALL_REWORK_TIME rt
                            WHERE rt.VIN_NO = vm.VIN_NO
                            ORDER BY rt.DATE_TIME DESC)
                            = 'TakeIn'
                            OR vm.TAKEOUT_STATION IS NULL)
                            AND sm.STATION_NAME IS NOT NULL
                            AND (vm.OPERATION_ACTION NOT IN ('A070', 'A080'))
                            AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND vm.MODEL_NO IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND vm.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (ProdOrder != "ALL" && ProdOrder != "")
            {
                sql += " AND vm.[ORDER] IN ( " + ProdOrder + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += @"
                    GROUP BY vm.OPERATION_ID,
                    sm.STATION_ID,
                    sm.STATION_NAME,
                    sm.DISPLAY_SEQ) in_line
					ON sm.STATION_ID = in_line.STATION_ID

                    UNION

                    SELECT DISTINCT
                      rework_data.STATION_ID,
                      rework_data.STATION_NAME,
                      rework_data.DISPLAY_SEQ,
                      rework_data.[IN_LINE],
                      SUM(rework_data.F1_REWORK) OVER (PARTITION BY rework_data.STATION_ID) AS [F1_REWORK],
                      SUM(rework_data.F2_REWORK) OVER (PARTITION BY rework_data.STATION_ID) AS [F2_REWORK],
                      rework_data.[F2_CASCADE],
                      SUM(rework_data.[COUNT]) OVER (PARTITION BY rework_data.STATION_ID) AS [COUNT]
                    FROM (SELECT
                      sm.STATION_ID,
                      sm.STATION_NAME,
                      sm.DISPLAY_SEQ,
                      0 AS [IN_LINE],
                      CASE
                        WHEN (vm.TAKEOUT_STATION = 'Q05') THEN COUNT(vm.VIN_NO)
                        ELSE NULL
                      END AS [F1_REWORK],
                      CASE
                        WHEN (vm.TAKEOUT_STATION = 'Q08') THEN COUNT(vm.VIN_NO)
                        ELSE NULL
                      END AS [F2_REWORK],
                      0 AS [F2_CASCADE],
                      COUNT(vm.VIN_NO) AS [COUNT]
                    FROM VIEW_ALL_VEHICLEMST VM
                    LEFT JOIN (
                        SELECT DISTINCT
                          STATION_ID,
                          STATION_NAME,
                          DISPLAY_SEQ
                        FROM STATIONMST
                        WHERE STATUS = 'Y'
                        AND (BROADCAST_FLAG <> 'Y'
                        OR BROADCAST_FLAG IS NULL)
                        AND STATION_ID NOT IN ('Q01')
                    ) sm
                    ON sm.STATION_ID = vm.OPERATION_ID
                    LEFT JOIN MODELMST mm
                    ON mm.MATL_GROUP = vm.MODEL_NO
                    WHERE (SELECT TOP 1
                      rt.ACTION
                    FROM VIEW_ALL_REWORK_TIME rt
                    WHERE rt.VIN_NO = vm.VIN_NO
                    ORDER BY rt.DATE_TIME DESC)
                    <> 'TakeIn'
                    AND vm.TAKEOUT_STATION IS NOT NULL
                    AND sm.STATION_NAME IS NOT NULL
                    AND (vm.OPERATION_ACTION NOT IN ('A070', 'A080')
                    OR vm.OPERATION_ACTION IS NULL)
                    AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND vm.MODEL_NO IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (ProdOrder != "ALL" && ProdOrder != "")
            {
                sql += " AND vm.[ORDER] IN ( " + ProdOrder + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += @"
                    GROUP BY sm.STATION_ID,
                    sm.STATION_NAME,
                    sm.DISPLAY_SEQ,
                    vm.TAKEOUT_STATION) rework_data
                    )) allData

                    LEFT JOIN (SELECT
                      COUNT(cas.VIN_NO) AS [CAS_COUNT],
                      cas.OPERATION_ID
                    FROM (SELECT DISTINCT
                      VM.VIN_NO,
                      VM.OPERATION_ID,
                      CASE
                        WHEN (GETDATE() > vm.CASCADE_DATETIME AND
                          DATEDIFF(MINUTE, vm.CASCADE_DATETIME, GETDATE()) > 5) THEN VM.CASCADE_DATETIME
                        ELSE NULL
                      END AS CASCADE_DATETIME
                    FROM VIEW_ALL_VEHICLEMST VM
                    WHERE VM.CASCADE_DATETIME IS NOT NULL
                    AND VM.OPERATION_ACTION NOT IN ('A070', 'A080')
                    AND VM.OPERATION_ID NOT IN ('Q10')
                    AND VM.CHASSIS_NO NOT IN (SELECT VIN FROM SAP_UPDATE WHERE ACTION_CODE IN ('A070','A080'))
            --(SELECT DISTINCT
                   --   STATION_ID
                  --  FROM STATIONMST
                   -- WHERE MAIN_SUB = 'SUB')";

            if (Lot != "ALL" && Lot != "")
            {
                sql += " AND vm.WBS_ELEM IN ( " + Lot + " ) ";
            }
            if (Color != "ALL" && Color != "")
            {
                sql += " AND vm.COLOR_CODE IN ( " + Color + " ) ";
            }
            if (Model != "ALL" && Model != "")
            {
                sql += " AND vm.MODEL_NO IN ( " + Model + " ) ";
            }
            if (BasicCode != "ALL" && BasicCode != "")
            {
                sql += " AND VM.MATL_GROUP_DESC IN ( " + BasicCode + " ) ";
            }
            if (ProdOrder != "ALL" && ProdOrder != "")
            {
                sql += " AND vm.[ORDER] IN ( " + ProdOrder + " ) ";
            }
            if (chassisno != "%%")
            {
                sql += " AND vm.CHASSIS_NO LIKE ( " + db.GetStr(chassisno) + " ) ";
            }
            if (engineNo != "%%")
            {
                sql += " AND vm.ENGINE_NO LIKE ( " + db.GetStr(engineNo) + " ) ";
            }
            if (lineId != "")
            {
                sql += " AND mm.LINE IN (" + lineId + ") ";
            }
            if (ShiftTime != null && ShiftTime.Count == 2)
            {
                TimeSpan startTime = ShiftTime[0];
                TimeSpan endTime = ShiftTime[1];
                hashtable["@START_TIME"] = startTime;
                hashtable["@END_TIME"] = endTime;

                if (startTime < endTime)
                {
                    sql += @"
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME
                        AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME
                    ";
                }
                else
                {
                    sql += @"
                        AND
                        (
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= @START_TIME AND CONVERT(time(7),VM.UPDATE_DATETIME) <='23:59:59.0000000')
	                        OR
	                        (CONVERT(time(7),VM.UPDATE_DATETIME) >= '00:00:00.0000000' AND CONVERT(time(7),VM.UPDATE_DATETIME) < @END_TIME)
                        )
                    ";
                }
            }

            sql += @"
                    GROUP BY VM.OPERATION_ID,
                             VM.VIN_NO,
                             VM.CASCADE_DATETIME) cas
                    WHERE cas.CASCADE_DATETIME IS NOT NULL
                    GROUP BY cas.OPERATION_ID) cas_data
                      ON allData.STATION_ID = cas_data.OPERATION_ID

                    GROUP BY allData.STATION_ID,
                             allData.STATION_NAME,
                             allData.DISPLAY_SEQ,
                             allData.IN_LINE,
                             allData.F1_REWORK,
                             allData.F2_REWORK,
                             allData.F2_CASCADE,
                             allData.[COUNT],
                             cas_data.CAS_COUNT
                    )) wip_data
                    WHERE 1 = 1";

            if (rework != "All" && rework != "")
            {
                if (rework.Equals("'F1 Rework'"))
                {
                    sql += @" AND wip_data.[F1_REWORK] IS NOT NULL";
                }

                if (rework.Equals("'F2 Rework'"))
                {
                    sql += @" AND wip_data.[F2_REWORK] IS NOT NULL";
                }
            }

            if (!string.IsNullOrEmpty(station))
            {
                sql += @" AND wip_data.STATION_ID IN (" + station + ") ";
            }

            sql += @" ORDER BY DISPLAY_SEQ";

            return retrieveData(sql, hashtable);
        }

        public DataTable getAllLot()
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT VM.WBS_ELEM AS LOT ";
            strSQL += " FROM VEHICLEMST VM ";
            strSQL += " UNION ";
            strSQL += " SELECT QR.LOT_NO AS LOT ";
            strSQL += " FROM QRINFO QR, VEHICLE_HISTORY VH ";
            strSQL += " WHERE QR.VIN_NO = VH.VIN_NO ";
            strSQL += " AND ((SELECT COUNT(*) FROM VEHICLEMST WHERE VIN_NO = QR.VIN_NO AND VIN_NO = VH.VIN_NO)=0) ";
            strSQL += " ORDER BY VM.WBS_ELEM ASC";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getViewAllLot()
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT VM.WBS_ELEM AS LOT ";
            strSQL += " FROM VIEW_ALL_VEHICLEMST VM ";
            strSQL += " UNION ";
            strSQL += " SELECT QR.LOT_NO AS LOT ";
            strSQL += " FROM VIEW_ALL_QRINFO QR, VIEW_ALL_VEHICLE_HISTORY VH ";
            strSQL += " WHERE QR.VIN_NO = VH.VIN_NO ";
            strSQL += " AND ((SELECT COUNT(*) FROM VIEW_ALL_VEHICLEMST WHERE VIN_NO = QR.VIN_NO AND VIN_NO = VH.VIN_NO)=0) ";
            strSQL += " ORDER BY VM.WBS_ELEM ASC";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable checkTrimStartStation(Hashtable sqlParams)
        {
            string strSQL = string.Empty;

            strSQL += @"
                SELECT *
                FROM STATIONMST
                WHERE STATION_ID = @checkStationId
                AND LINE_DESC = 'Start'
            ";

            //db.GetDataTable(strSQL, out dt);
            return retrieveData(strSQL, sqlParams);
        }

        public DataTable getAllModelCode(string Lot)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT MODEL_CODE ";
            strSQL += " FROM ( ";
            strSQL += " SELECT VM.WBS_ELEM AS LOT, VM.MODEL_NO AS MODEL_CODE ";
            strSQL += " FROM VEHICLEMST VM ";
            strSQL += " UNION ";
            strSQL += " SELECT QR.LOT_NO AS LOT, m.MATL_GROUP AS MODEL_CODE ";
            strSQL += " FROM QRINFO QR, VEHICLE_HISTORY VH, MODELMST m ";
            strSQL += " WHERE QR.VIN_NO = VH.VIN_NO ";
            strSQL += " AND ((SELECT COUNT(*) FROM VEHICLEMST WHERE VIN_NO = QR.VIN_NO AND VIN_NO = VH.VIN_NO)=0) AND m.MODEL_ID = QR.MODEL_ID)A ";
            if (Lot != "ALL" && Lot != "")
                strSQL += " WHERE LOT IN ( " + Lot + " ) ";
            //strSQL += " WHERE LOT = " + db.GetStr(Lot);
            strSQL += " ORDER BY MODEL_CODE ASC";

            //db.GetDataTable(strSQL, out dt);
            dt = retrieveData(strSQL, null);
            return dt;
        }

        public DataTable getViewAllModelCode(string Lot)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT MODEL_CODE ";
            strSQL += " FROM ( ";
            strSQL += " SELECT VM.WBS_ELEM AS LOT, VM.MODEL_NO AS MODEL_CODE ";
            strSQL += " FROM VIEW_ALL_VEHICLEMST VM ";
            strSQL += " UNION ";
            strSQL += " SELECT QR.LOT_NO AS LOT, m.MATL_GROUP AS MODEL_CODE ";
            strSQL += " FROM VIEW_ALL_QRINFO QR, VIEW_ALL_VEHICLE_HISTORY VH, MODELMST m ";
            strSQL += " WHERE QR.VIN_NO = VH.VIN_NO ";
            strSQL += " AND ((SELECT COUNT(*) FROM VIEW_ALL_VEHICLEMST WHERE VIN_NO = QR.VIN_NO AND VIN_NO = VH.VIN_NO)=0) AND m.MODEL_ID = QR.MODEL_ID)A ";
            if (Lot != "ALL" && Lot != "")
                strSQL += " WHERE LOT IN ( " + Lot + " ) ";
            //strSQL += " WHERE LOT = " + db.GetStr(Lot);
            strSQL += " ORDER BY MODEL_CODE ASC";

            //db.GetDataTable(strSQL, out dt);
            dt = retrieveData(strSQL, null);
            return dt;
        }

        public DataTable getColor(string model)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT COLOR_CODE AS COLOR ";
            strSQL += " FROM VEHICLEMST ";
            if (model != "ALL" && model != "")
                strSQL += " WHERE MODEL_NO IN ( " + model + " ) ";
            //strSQL += " WHERE MODEL_NO = " + db.GetStr(model);
            strSQL += " ORDER BY COLOR_CODE ASC ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getViewAllColor(string model)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT COLOR_CODE AS COLOR ";
            strSQL += " FROM VIEW_ALL_VEHICLEMST ";
            if (model != "ALL" && model != "")
                strSQL += " WHERE MODEL_NO IN ( " + model + " ) ";
            //strSQL += " WHERE MODEL_NO = " + db.GetStr(model);
            strSQL += " ORDER BY COLOR_CODE ASC ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getBasicCode(string model)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT MATL_GROUP_DESC AS MSC ";
            strSQL += " FROM VEHICLEMST ";
            if (model != "ALL" && model != "")
                strSQL += " WHERE MODEL_NO IN ( " + model + " ) ";
            //strSQL += " WHERE MODEL_NO = " + db.GetStr(model);
            strSQL += " ORDER BY MATL_GROUP_DESC ASC ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getViewAllBasicCode(string model)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT MATL_GROUP_DESC AS MSC ";
            strSQL += " FROM VIEW_ALL_VEHICLEMST ";
            if (model != "ALL" && model != "")
                strSQL += " WHERE MODEL_NO IN ( " + model + " ) ";
            //strSQL += " WHERE MODEL_NO = " + db.GetStr(model);
            strSQL += " ORDER BY MATL_GROUP_DESC ASC ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getProdOrder()
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT [ORDER] AS PROD_ORDER ";
            strSQL += " FROM VEHICLEMST ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getViewAllProdOrder()
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT [ORDER] AS PROD_ORDER ";
            strSQL += " FROM VIEW_ALL_VEHICLEMST ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        //Comment by KCC 20170221
        /*public DataTable getStation()
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT STATION_ID, STATION_NAME";
            strSQL += " FROM STATIONMST ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }*/

        public DataTable getAllStation()
        {
            DataTable dt = null;
            string strSQL = string.Empty;

            #region old query

            //strSQL += @"
            //    SELECT DISTINCT A.SORTING_SEQ,
            //    STUFF((SELECT DISTINCT ',' + CONVERT(VARCHAR(12), sm.STATION_ID)
            //     FROM STATIONMST sm
            //     WHERE A.SORTING_SEQ = sm.SORTING_SEQ
            //    FOR XML PATH('')), 1, 1, '') AS STATION_ID,
            //    STUFF((SELECT DISTINCT ',' + CONVERT(VARCHAR(12), sm.STATION_NAME)
            //     FROM STATIONMST sm
            //     WHERE A.SORTING_SEQ = sm.SORTING_SEQ
            //    FOR XML PATH('')), 1, 1, '') AS STATION_NAME
            //    FROM
            //    (
            //     SELECT DISTINCT STATION_ID, STATION_NAME, SORTING_SEQ
            //     FROM STATIONMST
            //     WHERE STATUS = 'Y'
            //     AND (BROADCAST_FLAG IS NULL OR BROADCAST_FLAG = 'N')
            //     AND (LINE_DESC NOT IN('REWORK', 'PBSI') OR LINE_DESC IS NULL )

            //    )A
            //    ORDER BY SORTING_SEQ
            //";

            #endregion old query

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getStation(string lineId)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL += @"
                SELECT DISTINCT STATION_ID, STATION_NAME, MAX(SORTING_SEQ) AS SORTING_SEQ
                FROM STATIONMST
                WHERE STATUS = 'Y'
            ";

            if (lineId != "")
            {
                strSQL += @"
                    AND STATION_GROUP IN ( " + lineId + @") ";
            }

            //strSQL += @"
            //    AND (BROADCAST_FLAG IS NULL OR BROADCAST_FLAG = 'N')
            //    AND (LINE_DESC NOT IN('REWORK', 'PBSI') OR LINE_DESC IS NULL )
            //    ORDER BY SORTING_SEQ
            //";

            strSQL += @"
                AND (BROADCAST_FLAG IS NULL OR BROADCAST_FLAG = 'N')
                AND (LINE_DESC NOT IN('REWORK') OR LINE_DESC IS NULL )
                GROUP BY STATION_ID, STATION_NAME
                ORDER BY SORTING_SEQ
            ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        //Added by Xian 21/11/2020
        public DataTable getReworkStation()
        {
            DataTable dt = new DataTable();
            string strSQL = @"SELECT DISTINCT STATION_ID, STATION_NAME FROM STATIONMST WHERE LINE_DESC = 'REWORK' AND STATUS = 'Y'";
            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        //BO JUN add OP Action Filter
        public DataTable getOpAction()
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT DISTINCT OPERATION_ACTION ";
            strSQL += " FROM STATIONMST ";
            strSQL += " WHERE OPERATION_ACTION IS NOT NULL ";
            strSQL += " ORDER BY OPERATION_ACTION ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getLine()
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL += @"
                SELECT DISTINCT LINE_ID, LINE_NAME
                FROM LINE
            ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getShift()
        {
            string strSQL = string.Empty;
            strSQL = @"
            SELECT SHIFT_ID, SHIFT_NAME FROM SHIFTMST
            ";
            return retrieveData(strSQL, null);
        }

        public DataTable getallchassis(string modelno, string porder)
        {
            DataTable dt = null;
            string strSQL = string.Empty;
            strSQL = " SELECT v.CHASSIS_NO ";
            strSQL += " FROM  VEHICLEMST v, MODELMST m ";
            strSQL += " WHERE CHASSIS_NO IS NOT NULL ";
            strSQL += " AND v.MODEL_NO = m.MATL_GROUP";
            if (modelno != "ALL")
                strSQL += " AND m.MATL_GROUP =" + db.GetStr(modelno);
            if (porder != "ALL")
                strSQL += " AND v.[ORDER] = " + db.GetStr(porder);
            strSQL += " ORDER BY v.CHASSIS_NO ";
            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public string checkVehicleExist(Hashtable sqlParams)
        {
            string strProductionSql = @"
                SELECT *
                FROM VEHICLEMST
                WHERE CHASSIS_NO LIKE @chassisNo
            ";

            string strArchiveSql = @"
                SELECT *
                FROM BMW_VQS_ARCHIVING_DB_V1.dbo.VEHICLEMST
                WHERE CHASSIS_NO LIKE @chassisNo
            ";

            if (retrieveData(strProductionSql, sqlParams).Rows.Count > 0)
            {
                return "Production";
            }
            else if (retrieveData(strArchiveSql, sqlParams).Rows.Count > 0)
            {
                return "Archive";
            }

            return "False";
        }

        public DataTable getAllChassisForAutoComplete(string chassis)
        {
            DataTable dt = null;
            string strSQL = "";
            strSQL = " SELECT DISTINCT v.CHASSIS_NO ";
            strSQL += " FROM VEHICLEMST v ";
            strSQL += " WHERE CHASSIS_NO IS NOT NULL ";
            strSQL += " AND v.CHASSIS_NO LIKE '%" + chassis + "%' ";
            strSQL += " ORDER BY v.CHASSIS_NO ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getViewAllChassisForAutoComplete(string chassis)
        {
            DataTable dt = null;
            string strSQL = "";
            strSQL = " SELECT DISTINCT v.CHASSIS_NO ";
            strSQL += " FROM VIEW_ALL_VEHICLEMST v ";
            strSQL += " WHERE CHASSIS_NO IS NOT NULL ";
            strSQL += " AND v.CHASSIS_NO LIKE '%" + chassis + "%' ";
            strSQL += " ORDER BY v.CHASSIS_NO ";

            db.GetDataTable(strSQL, out dt);
            return dt;
        }

        public DataTable getAllEngineForAutoComplete(string engineNo)
        {
            Hashtable sqlParams = new Hashtable();
            sqlParams["@ENGINE_NO"] = engineNo;

            string strSql = @"
            SELECT ENGINE_NO
            FROM VEHICLEMST
            WHERE ENGINE_NO LIKE '%' + @ENGINE_NO + '%'
            ";

            return retrieveData(strSql, sqlParams);
        }

        public DataTable getViewAllEngineForAutoComplete(string engineNo)
        {
            Hashtable sqlParams = new Hashtable();
            sqlParams["@ENGINE_NO"] = engineNo;

            string strSql = @"
            SELECT ENGINE_NO
            FROM VIEW_ALL_VEHICLEMST
            WHERE ENGINE_NO LIKE '%' + @ENGINE_NO + '%'
            ";

            return retrieveData(strSql, sqlParams);
        }

        public List<TimeSpan> GetShiftTime(string ShiftID)
        {
            /*
             0 = START_TIME
             1 = END_TIME
            */
            List<TimeSpan> listDT = new List<TimeSpan>();

            Hashtable hashtable = new Hashtable();
            hashtable["@SHIFT_ID"] = ShiftID;
            string sql = @"
            SELECT sm.START_TIME,

            CASE WHEN (sm.START_TIME > sm.END_TIME)
            THEN
	            (SELECT MIN(sm2.START_TIME) FROM SHIFTMST sm2)
            ELSE
	            (SELECT MIN(sm2.START_TIME) FROM SHIFTMST sm2 WHERE sm2.START_TIME > sm.END_TIME)
            END AS END_TIME

            FROM SHIFTMST sm WHERE SHIFT_ID = @SHIFT_ID
            ";
            DataTable ShiftDt = new DataTable();
            if (ShiftID != null)
            {
                if (ShiftID != "" && ShiftID != "ALL")
                {
                    ShiftDt = retrieveData(sql, hashtable);
                }
            }

            if (ShiftDt.Rows.Count > 0)
            {
                listDT.Add((TimeSpan)ShiftDt.Rows[0]["START_TIME"]);
                listDT.Add((TimeSpan)ShiftDt.Rows[0]["END_TIME"]);
                return listDT;
            }
            else
            {
                return listDT;
            }
        }

        public List<String> GetAgingRange(string AgingID)
        {
            /*
             0 = MIN_AGE
             1 = MAX_AGE
            */
            List<String> listDT = new List<String>();

            Hashtable hashtable = new Hashtable();
            hashtable["@AGING_ID"] = AgingID;
            string sql = @"
            SELECT MIN_AGE, MAX_AGE FROM AGINGMST WHERE AGING_ID = @AGING_ID
            ";
            DataTable AgingDt = new DataTable();
            if (AgingID != null)
            {
                if (AgingID != "" && AgingID != "ALL")
                {
                    AgingDt = retrieveData(sql, hashtable);
                }
            }

            if (AgingDt.Rows.Count > 0)
            {
                listDT.Add((String)AgingDt.Rows[0]["MIN_AGE"]);
                listDT.Add((String)AgingDt.Rows[0]["MAX_AGE"]);
                return listDT;
            }
            else
            {
                return listDT;
            }
        }

        public string CheckVehicleExist(string userId, string chassis)
        {
            CommonMethod common = new CommonMethod();
            string methodName = common.MethodName();

            try
            {
                return common.CheckVehicleExist(userId, chassis);
            }
            catch (Exception ex)
            {
                common.LogWebError(LogType.ERROR_TYPE, userId, methodName, ex.ToString());
                return null;
            }
        }
    }
}