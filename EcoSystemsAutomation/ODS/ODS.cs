using Oracle.ManagedDataAccess.Client;
using ClosedXML.Excel;
using System.Data;
using System.Linq.Expressions;

public class ODS : Iods
{
    string inputExcelPath;
    private readonly IConfiguration _configuration;
    public ODS(IConfiguration configuration)
    {
        _configuration = configuration;

    }


    public void ExtractPolicies(List<string> policyNumbers)
    {
        string policynumbers = string.Join(",", policyNumbers.ConvertAll(num => $"'{num}'"));

        using (OracleConnection conn = new OracleConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            try
            {


                conn.Open();
                Console.WriteLine("Connected to Oracle Database!");

                // Example: Execute a query
                string query = @"SELECT DISTINCT
    p.SRC_POLICY_NUMBER,
    c.SRC_CLAIM_NUMBER,
    ph.PHID,
    p.BILLING_ACCOUNT_NUMBER,
    pd.RATING_STATE,
    A.AGENCYNBR,
    A.AGENCYNAME,
    SUM(pd.WRITTEN_PREMIUM) AS TOTAL_WRITTEN_PREMIUM
FROM 
    ods.policy p
    JOIN ods.policy_premium_detail pd 
        ON pd.policy_id = p.POLICY_ID
    JOIN CRT.MLCRTPXP po 
        ON po.POLICY_NBR = p.SRC_POLICY_NUMBER
    JOIN CRT.MLABXRXP AX 
        ON AX.AGENCYNBR = po.AGENCYNBR
    JOIN CRT.MLCRTAXP A 
        ON A.AGENCYNBR = AX.AGENCYNBR
    JOIN ods.POLICYHOLDER ph 
        ON ph.POLHDR_ID = p.POLHDR_ID
    LEFT OUTER JOIN ods.CLAIM c 
        ON c.POLICY_ID = p.POLICY_ID
WHERE     p.SRC_SYSTEM = 'DCP' 
    AND p.SRC_POLICY_NUMBER in(" + policynumbers + ") GROUP BY p.SRC_POLICY_NUMBER,c.SRC_CLAIM_NUMBER,ph.PHID,p.BILLING_ACCOUNT_NUMBER,pd.RATING_STATE,A.AGENCYNBR,A.AGENCYNAME";
                using (OracleCommand cmd = new OracleCommand(query, conn))
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    inputExcelPath = _configuration["InputExcel"];

                    // Export DataTable to Excel
                    var odsFolder = createODSFolder();
                    //ExportDataTableToSingleExcelFile(dataTable, odsFolder);
                    //ExportEachRowToExcel(dataTable, odsFolder);
                    ExportRowsByLOBToExcel(dataTable, odsFolder);
                    EnrichGLPoliciesWithAdditionalData(inputExcelPath, odsFolder);
                    Console.WriteLine("Data exported successfully to ODSDump.xlsx!");
                }
            }
            catch (Exception ex)
            {

            }
        }


    }

    private string createODSFolder()

    {
        string parentFolderPath = Path.GetDirectoryName(_configuration["InputExcel"]);
        string odsFolderPath = Path.Combine(parentFolderPath, DateTime.Now.ToString("MM-dd-yyyy"), "ODS");

        try
        {
            // Check if the parent folder exists
            if (!Directory.Exists(parentFolderPath))
            {
                Console.WriteLine($"Parent folder does not exist: {parentFolderPath}");
                return "";
            }

            // If the folder with today's date exists, delete it and its contents
            if (Directory.Exists(odsFolderPath))
            {
                Console.WriteLine($"Folder already exists for today's date. Deleting folder: {odsFolderPath}");
                Directory.Delete(odsFolderPath, true); // true to delete contents as well
            }

            // Create the ODS folder if it doesn't exist
            if (!Directory.Exists(odsFolderPath))
            {
                Directory.CreateDirectory(odsFolderPath);
                Console.WriteLine($"ODS folder created: {odsFolderPath}");
            }
            else
            {
                Console.WriteLine($"ODS folder already exists: {odsFolderPath}");
            }
        }
        catch (Exception ex) { }
        return odsFolderPath;
    }


    static void ExportDataTableToSingleExcelFile(DataTable dataTable, string folderPath)
    {
        // Check if the folder exists, create it if necessary
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Define the file name and full path for the output Excel file
        string filePath = Path.Combine(folderPath, "ODSDump.xlsx");

        using (var workbook = new XLWorkbook())
        {
            // Add a worksheet
            var worksheet = workbook.Worksheets.Add("Data");

            // Add the headers
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                worksheet.Cell(1, col + 1).Value = dataTable.Columns[col].ColumnName;
            }

            // Add all rows of data
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = dataTable.Rows[row][col]?.ToString();
                }
            }

            // Save the workbook
            workbook.SaveAs(filePath);
        }
    }


    static void ExportEachRowToExcel(DataTable dataTable, string folderPath)
    {
        // Check if the folder exists, create it if necessary
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Validate if the table contains the "SRC_POLICY_NUMBER" column
        if (!dataTable.Columns.Contains("SRC_POLICY_NUMBER"))
        {
            throw new ArgumentException("The DataTable must contain a 'SRC_POLICY_NUMBER' column.");
        }

        foreach (DataRow row in dataTable.Rows)
        {
            // Get the file name from the SRC_POLICY_NUMBER column
            string fileName = row["SRC_POLICY_NUMBER"].ToString();

            // Ensure the file name is valid
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

            // Create the full file path
            string filePath = Path.Combine(folderPath, $"{fileName}.xlsx");

            using (var workbook = new XLWorkbook())
            {
                // Add a worksheet
                var worksheet = workbook.Worksheets.Add("Data");

                // Add the headers
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    worksheet.Cell(1, col + 1).Value = dataTable.Columns[col].ColumnName;
                }

                // Add the current row's data
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    worksheet.Cell(2, col + 1).Value = row[col]?.ToString();
                }

                // Save the workbook
                workbook.SaveAs(filePath);
            }

            Console.WriteLine($"Excel file created: {filePath}");
        }
    }


    static void ExportRowsByLOBToExcel(DataTable dataTable, string folderPath)
    {
        // Check if the folder exists, create it if necessary
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Validate if the table contains the "SRC_POLICY_NUMBER" column
        if (!dataTable.Columns.Contains("SRC_POLICY_NUMBER"))
        {
            throw new ArgumentException("The DataTable must contain a 'SRC_POLICY_NUMBER' column.");
        }

        // Separate rows by LOB
        var glRows = dataTable.AsEnumerable()
                              .Where(row => row["SRC_POLICY_NUMBER"].ToString().StartsWith("GL"))
                              .CopyToDataTable();

        var cxRows = dataTable.AsEnumerable()
                              .Where(row => row["SRC_POLICY_NUMBER"].ToString().StartsWith("CX"))
                              .CopyToDataTable();

        // Export GL rows to an Excel file
        ExportDataTableToExcel(glRows, folderPath, "GL_Policies.xlsx");

        // Export CX rows to an Excel file
        ExportDataTableToExcel(cxRows, folderPath, "CX_Policies.xlsx");
    }


    public void EnrichGLPoliciesWithAdditionalData(string inputExcelPath, string odsFolder)
    {
        try
        {
            // Create a dictionary to store unique policy data
            Dictionary<string, (DateTime effectiveDate, string mailingAddress)> policyData = new Dictionary<string, (DateTime, string)>();

            // Read input Excel file
            using (var workbook = new XLWorkbook(inputExcelPath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed();

                // Find column indices
                int policyNumberCol = -1;
                int effectiveDateCol = -1;
                int mailingAddressCol = -1;

                var headerRow = worksheet.Row(1);
                for (int col = 1; col <= headerRow.CellsUsed().Count(); col++)
                {
                    string headerValue = headerRow.Cell(col).Value.ToString().Trim();
                    if (headerValue.Equals("Policy#", StringComparison.OrdinalIgnoreCase))
                        policyNumberCol = col;
                    else if (headerValue.Equals("Effective Date", StringComparison.OrdinalIgnoreCase))
                        effectiveDateCol = col;
                    else if (headerValue.Equals("Mailing Address", StringComparison.OrdinalIgnoreCase))
                        mailingAddressCol = col;
                }

                // Verify that all required columns were found
                if (policyNumberCol == -1 || effectiveDateCol == -1 || mailingAddressCol == -1)
                {
                    throw new Exception("Required columns not found in input Excel file");
                }

                // Read data starting from row 2
                foreach (var row in rows.Skip(1))
                {
                    string policyNumber = row.Cell(policyNumberCol).Value.ToString().Trim();
                    if (!string.IsNullOrEmpty(policyNumber))
                    {
                        DateTime effectiveDate;
                        if (!DateTime.TryParse(row.Cell(effectiveDateCol).Value.ToString(), out effectiveDate))
                        {
                            effectiveDate = DateTime.MinValue;
                        }
                        string mailingAddress = row.Cell(mailingAddressCol).Value.ToString().Trim();

                        // Only store if we haven't seen this policy number before
                        if (!policyData.ContainsKey(policyNumber))
                        {
                            policyData.Add(policyNumber, (effectiveDate, mailingAddress));
                        }
                    }
                }
            }

            // Update GL_Policies.xlsx
            string glPoliciesPath = Path.Combine(odsFolder, "GL_Policies.xlsx");

            if (!File.Exists(glPoliciesPath))
            {
                throw new Exception($"GL_Policies.xlsx not found at: {glPoliciesPath}");
            }

            using (var workbook = new XLWorkbook(glPoliciesPath))
            {
                var worksheet = workbook.Worksheet(1);
                var headerRow = worksheet.Row(1);

                // Get current number of columns
                int lastColumn = worksheet.ColumnsUsed().Count();

                // Find all column indices we need
                int glPolicyNumberCol = -1;
                Dictionary<string, int> columnIndices = new Dictionary<string, int>();

                for (int col = 1; col <= lastColumn; col++)
                {
                    string columnName = headerRow.Cell(col).Value.ToString().Trim();
                    if (columnName.Equals("SRC_POLICY_NUMBER", StringComparison.OrdinalIgnoreCase))
                    {
                        glPolicyNumberCol = col;
                    }
                    columnIndices[columnName] = col;
                }

                if (glPolicyNumberCol == -1)
                {
                    throw new Exception("SRC_POLICY_NUMBER column not found in GL_Policies.xlsx");
                }

                // Create a new workbook for the deduplicated data
                var newWorkbook = new XLWorkbook();
                var newWorksheet = newWorkbook.AddWorksheet("Sheet1");

                // Copy header row
                for (int col = 1; col <= lastColumn; col++)
                {
                    newWorksheet.Cell(1, col).Value = headerRow.Cell(col).Value;
                }

                // Add new columns if they don't exist
                if (!columnIndices.ContainsKey("Effective_Date"))
                {
                    lastColumn++;
                    newWorksheet.Cell(1, lastColumn).Value = "EFFECTIVE_DATE";
                }
                if (!columnIndices.ContainsKey("Mailing_Address"))
                {
                    lastColumn++;
                    newWorksheet.Cell(1, lastColumn).Value = "MAILING_ADDRESS";
                }

                // Process data rows and remove duplicates
                var processedPolicies = new HashSet<string>();
                int newRow = 2; // Start from row 2 in the new worksheet

                var dataRows = worksheet.RowsUsed().Skip(1);
                foreach (var row in dataRows)
                {
                    string policyNumber = row.Cell(glPolicyNumberCol).Value.ToString().Trim();

                    if (!processedPolicies.Contains(policyNumber))
                    {
                        processedPolicies.Add(policyNumber);

                        // Copy all existing columns
                        for (int col = 1; col <= lastColumn; col++)
                        {
                            if (col <= worksheet.ColumnsUsed().Count())
                            {
                                newWorksheet.Cell(newRow, col).Value = row.Cell(col).Value;
                            }
                        }

                        // Add effective date and mailing address if we have them
                        if (policyData.ContainsKey(policyNumber))
                        {
                            var (effectiveDate, mailingAddress) = policyData[policyNumber];

                            // Find or use new column positions
                            int effectiveDateCol = columnIndices.GetValueOrDefault("Effective_Date", lastColumn - 1);
                            int mailingAddressCol = columnIndices.GetValueOrDefault("Mailing_Address", lastColumn);

                            newWorksheet.Cell(newRow, effectiveDateCol).Value =
                                effectiveDate != DateTime.MinValue ? effectiveDate.ToString("MM/dd/yyyy") : "";
                            newWorksheet.Cell(newRow, mailingAddressCol).Value = mailingAddress;
                        }

                        newRow++;
                    }
                }

                // Auto-fit columns
                newWorksheet.Columns().AdjustToContents();

                // Save the new workbook
                newWorkbook.SaveAs(glPoliciesPath);
                Console.WriteLine($"Successfully updated GL_Policies.xlsx with {processedPolicies.Count} unique policies at: {glPoliciesPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in EnrichGLPoliciesWithAdditionalData: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw;
        }
    }

    static void ExportDataTableToExcel(DataTable dataTable, string folderPath, string fileName)
    {
        // Create the full file path
        string filePath = Path.Combine(folderPath, fileName);

        using (var workbook = new XLWorkbook())
        {
            // Add a worksheet
            var worksheet = workbook.Worksheets.Add("Data");

            // Add the headers
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                worksheet.Cell(1, col + 1).Value = dataTable.Columns[col].ColumnName;
            }

            // Add the rows' data
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = dataTable.Rows[row][col]?.ToString();
                }
            }

            // Save the workbook
            workbook.SaveAs(filePath);
        }

        Console.WriteLine($"Excel file created: {filePath}");
    }


}