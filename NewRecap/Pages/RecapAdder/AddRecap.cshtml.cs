using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewRecap.Model;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using NewRecap.MyAppHelper;
using System.Data;

namespace NewRecap.Pages.RecapAdder
{
    [Authorize]
    [BindProperties]
    public class AddRecapModel : PageModel
    {
        public Recap NewRecap { get; set; } = new Recap();
        public bool IsAdmin { get; set; }
        public List<EmployeeInfo> Employees { get; set; } = new List<EmployeeInfo>();
        public List<SelectListItem> Vehicles { get; set; } = new List<SelectListItem>();
        public int? SelectedVehicleID { get; set; }
        public List<int> SelectedEmployeeIds { get; set; } = new();
        public List<SelectListItem> EmployeeOptions { get; set; } = new();

        private const int ROLE_ADMIN = 2;


        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";
        public Cat6CableSegments CableCat6 { get; set; }
        public _182CableSegments Cable182 { get; set; } = new _182CableSegments();
        public _184CableSegments Cable184 { get; set; } = new _184CableSegments();
        public _186CableSegments Cable186 { get; set; } = new _186CableSegments();
        public FiberCableSegments CableFiber { get; set; } = new FiberCableSegments();
        public CoaxCableSegments CableCoax { get; set; } = new CoaxCableSegments();
        public string? TrainingEmployeeIds { get; set; }

        public void OnGet()
        {
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            //PopulateEmployeeList();
            PopulateVehicleList();
            PopulateEmployeeOptions();
            /*--------------------ADMIN PRIV----------------------*/
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {
            // Validate employee selection
            if (!SelectedEmployeeIds.Any())
            {
                ModelState.AddModelError("SelectedEmployeeIds", "Please select at least one employee.");
            }

            // === Compute totals directly from your start/end date+time fields ===
            var workSegs = NewRecap.WorkSegments;
            var driveSegs = NewRecap.TravelSegments;
            var lunchSegs = NewRecap.LunchSegments;
            var supportSegs = NewRecap.SupportSegments;
            var recapSegs = NewRecap.RecapSegments;

            // ========== DATE SPAN PER SEGMENT TYPE (end < start OR 2+ days apart) ==========

            // WORK
            if (workSegs.Any(s => SegmentDatesInvalid(s.WorkStartDate, s.WorkEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Work segment end date cannot be before the start date or 2+ days after the start date.");
            }

            // TRAVEL
            if (driveSegs.Any(s => SegmentDatesInvalid(s.DriveStartDate, s.DriveEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.TravelSegments",
                    "Travel segment end date cannot be before the start date or 2+ days after the start date.");
            }

            // LUNCH
            if (lunchSegs.Any(s => SegmentDatesInvalid(s.LunchStartDate, s.LunchEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Lunch segment end date cannot be before the start date or 2+ days after the start date.");
            }

            // SUPPORT
            if (supportSegs.Any(s => SegmentDatesInvalid(s.SupportStartDate, s.SupportEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.SupportSegments",
                    "Support segment end date cannot be before the start date or 2+ days after the start date.");
            }

            // RECAP
            if (recapSegs.Any(s => SegmentDatesInvalid(s.RecapStartDate, s.RecapEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Recap segment end date cannot be before the start date or 2+ days after the start date.");
            }


            // ========== COMPLETENESS RULES ==========
            // Complete = all 4 values (start date/time + end date/time) present

            bool hasCompleteWork = workSegs.Any(s =>
                IsComplete(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd));

            bool hasCompleteDrive = driveSegs.Any(s =>
                IsComplete(s.DriveStartDate, s.DriveStart, s.DriveEndDate, s.DriveEnd));

            bool hasCompleteLunch = lunchSegs.Any(s =>
                IsComplete(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd));

            bool hasCompleteSupport = supportSegs.Any(s =>
                IsComplete(s.SupportStartDate, s.SupportStart, s.SupportEndDate, s.SupportEnd));

            bool hasCompleteRecap = recapSegs.Any(s =>
                IsComplete(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd));

            // At least one complete (non-recap) segment of ANY type
            bool hasAtLeastOneComplete =
                hasCompleteWork || hasCompleteDrive || hasCompleteLunch || hasCompleteSupport;

            if (!hasAtLeastOneComplete)
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Please add at least one complete time segment (Work, Travel, Support, or Lunch) with both start and end date/time."
                );
            }

            // Require at least ONE complete Recap segment
            if (!hasCompleteRecap)
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Please add at least one complete Recap segment (start & end date/time)."
                );
            }

            // Require at least ONE complete Recap segment
            if (!hasCompleteRecap)
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Please add at least one complete Recap segment (start & end date/time)."
                );
            }

            // ========== INCOMPLETE SEGMENTS (partially filled rows) ==========

            if (workSegs.Any(s => IsIncomplete(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Work segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            if (driveSegs.Any(s => IsIncomplete(s.DriveStartDate, s.DriveStart, s.DriveEndDate, s.DriveEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.TravelSegments",
                    "Travel segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            if (lunchSegs.Any(s => IsIncomplete(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Lunch segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            if (supportSegs.Any(s => IsIncomplete(s.SupportStartDate, s.SupportStart, s.SupportEndDate, s.SupportEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.SupportSegments",
                    "Support segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            if (recapSegs.Any(s => IsIncomplete(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Recap segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            // ========== LUNCH vs OTHER TOTALS ==========

            double lunchTotal = lunchSegs.Sum(s =>
                Hours(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd));

            double workTotal = workSegs.Sum(s =>
                Hours(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd));

            double driveTotal = driveSegs.Sum(s =>
                Hours(s.DriveStartDate, s.DriveStart, s.DriveEndDate, s.DriveEnd));

            double supportTotal = supportSegs.Sum(s =>
                Hours(s.SupportStartDate, s.SupportStart, s.SupportEndDate, s.SupportEnd));

            double recapTotal = recapSegs.Sum(s =>
                Hours(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd));

            double overallOtherTotal = workTotal + driveTotal + supportTotal + recapTotal;

            // Lunch cannot be >= 2 hours total
            if (lunchTotal >= 2.0)
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Total lunch time cannot exceed 2 hours."
                );
            }

            // Lunch cannot exceed any other segment total
            if (overallOtherTotal > 0 && lunchTotal > overallOtherTotal)
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Total lunch time cannot exceed the total time recorded for the day."
                );
            }

            // If lunch is present but no non-lunch segments are complete, reject
            bool hasCompleteNonLunch = hasCompleteWork || hasCompleteDrive || hasCompleteSupport;
            if (hasCompleteLunch && !hasCompleteNonLunch)
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "You must include at least one Work, Travel, or Support segment if you log Lunch."
                );
            }


            // === Starting / Ending Mileage Validation ===
            var hasStartMileage = NewRecap.StartingMileage.HasValue;
            var hasEndMileage = NewRecap.EndingMileage.HasValue;

            // Case 1: one is filled, the other is null
            if (hasStartMileage ^ hasEndMileage) // XOR: exactly one is true
            {
                ModelState.AddModelError(
                    "NewRecap.StartingMileage",
                    "Starting and Ending mileage must both be provided or both left blank."
                );
                ModelState.AddModelError(
                    "NewRecap.EndingMileage",
                    "Starting and Ending mileage must both be provided or both left blank."
                );
            }

            // Case 2: both have values but ending <= starting
            if (hasStartMileage && hasEndMileage)
            {
                if (NewRecap.EndingMileage.Value <= NewRecap.StartingMileage.Value)
                {
                    ModelState.AddModelError(
                        "NewRecap.EndingMileage",
                        "Ending mileage must be greater than starting mileage."
                    );
                }
            }

            ValidateCablePair(
                nameof(CableCat6),                       // "CableCat6"
                nameof(Cat6CableSegments.Cat6Start),     // "Cat6Start"
                nameof(Cat6CableSegments.Cat6End),       // "Cat6End"
                CableCat6?.Cat6Start,
                CableCat6?.Cat6End);

            ValidateCablePair(
                nameof(Cable182),
                nameof(_182CableSegments.Cable182Start),
                nameof(_182CableSegments.Cable182End),
                Cable182?.Cable182Start,
                Cable182?.Cable182End);

            ValidateCablePair(
                nameof(Cable184),
                nameof(_184CableSegments.Cable184Start),
                nameof(_184CableSegments.Cable184End),
                Cable184?.Cable184Start,
                Cable184?.Cable184End);

            ValidateCablePair(
                nameof(Cable186),
                nameof(_186CableSegments.Cable186Start),
                nameof(_186CableSegments.Cable186End),
                Cable186?.Cable186Start,
                Cable186?.Cable186End);

            ValidateCablePair(
                nameof(CableFiber),
                nameof(FiberCableSegments.FiberStart),
                nameof(FiberCableSegments.FiberEnd),
                CableFiber?.FiberStart,
                CableFiber?.FiberEnd);

            ValidateCablePair(
                nameof(CableCoax),
                nameof(CoaxCableSegments.CoaxStart),
                nameof(CoaxCableSegments.CoaxEnd),
                CableCoax?.CoaxStart,
                CableCoax?.CoaxEnd);


            // ========== NON-POSITIVE DURATION (end <= start) ==========

            if (workSegs.Any(s => HasNonPositiveDuration(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Work segment end date/time must be after the start date/time.");
            }

            if (driveSegs.Any(s => HasNonPositiveDuration(s.DriveStartDate, s.DriveStart, s.DriveEndDate, s.DriveEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.TravelSegments",
                    "Travel segment end date/time must be after the start date/time.");
            }

            if (lunchSegs.Any(s => HasNonPositiveDuration(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Lunch segment end date/time must be after the start date/time.");
            }

            if (supportSegs.Any(s => HasNonPositiveDuration(s.SupportStartDate, s.SupportStart, s.SupportEndDate, s.SupportEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.SupportSegments",
                    "Support segment end date/time must be after the start date/time.");
            }

            if (recapSegs.Any(s => HasNonPositiveDuration(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Recap segment end date/time must be after the start date/time.");
            }

            HashSet<int> trainingIdSet = new HashSet<int>();

            if (!string.IsNullOrWhiteSpace(TrainingEmployeeIds))
            {
                var parts = TrainingEmployeeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var p in parts)
                {
                    if (int.TryParse(p, out int empId))
                    {
                        trainingIdSet.Add(empId);
                    }
                }
            }

            var description = (NewRecap.RecapDescription ?? string.Empty).Trim();
            var state = (NewRecap.RecapState ?? string.Empty).Trim();
            var city = (NewRecap.RecapCity ?? string.Empty).Trim();
            const int dbMaxDesc = 500;
            const int dbMaxState = 2;
            const int dbMaxCity = 50;


            if (description.Length > dbMaxDesc)
            {
                ModelState.AddModelError("NewRecap.RecapDescription", "Description must be at most 500 characters.");
            }

            if (state.Length > dbMaxState)
            {
                ModelState.AddModelError("NewRecap.RecapState", "State must be at most 2 characters.");
            }

            if (city.Length > dbMaxCity)
            {
                ModelState.AddModelError("NewRecap.RecapCity", "City must be at most 50 characters.");
            }

            if (ModelState.IsValid)
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    string cmdTextRecap = "INSERT INTO Recap (RecapWorkorderNumber, RecapDate, AddedBy, VehicleID, RecapDescription, RecapState, RecapCity, StartingMileage, EndingMileage, Customer) VALUES (@RecapWorkorderNumber, @RecapDate, @AddedBy, @VehicleID, @RecapDescription, @RecapState, @RecapCity, @StartingMileage, @EndingMileage, @Customer);";
                    SqlCommand cmdRecap = new SqlCommand(cmdTextRecap, conn);
                    cmdRecap.Parameters.AddWithValue("@RecapWorkorderNumber", NewRecap.RecapWorkorderNumber);
                    cmdRecap.Parameters.AddWithValue("@RecapDate", DateTime.Today);
                    cmdRecap.Parameters.AddWithValue("@AddedBy", userId);

                    var pv = cmdRecap.Parameters.Add("@VehicleID", SqlDbType.Int);
                    pv.Value = SelectedVehicleID.HasValue ? SelectedVehicleID.Value : DBNull.Value;

                    cmdRecap.Parameters.AddWithValue("@RecapDescription", NewRecap.RecapDescription);
                    cmdRecap.Parameters.AddWithValue("@RecapState", NewRecap.RecapState);
                    cmdRecap.Parameters.AddWithValue("@RecapCity", NewRecap.RecapCity);
                    var pSMileage = cmdRecap.Parameters.Add("@StartingMileage", SqlDbType.Int);
                    pSMileage.Value = NewRecap.StartingMileage.HasValue ? NewRecap.StartingMileage.Value : DBNull.Value;

                    var pSEnding = cmdRecap.Parameters.Add("@EndingMileage", SqlDbType.Int);
                    pSEnding.Value = NewRecap.EndingMileage.HasValue ? NewRecap.EndingMileage.Value : DBNull.Value;

                    cmdRecap.Parameters.AddWithValue("@Customer", NewRecap.Customer);


                    cmdRecap.ExecuteNonQuery();

                    int RecapID;
                    // Now fetch the generated AutoNumber (RecapID)
                    using (var idCmd = new SqlCommand("SELECT @@IDENTITY;", conn))
                    {
                        RecapID = Convert.ToInt32(idCmd.ExecuteScalar());
                    }

                    foreach (var empId in SelectedEmployeeIds)
                    {
                        bool isTraining = trainingIdSet.Contains(empId);
                         string cmdTextEmployeeRecap = "INSERT INTO EmployeeRecaps (RecapID, EmployeeID, IsTraining) VALUES (@RecapID, @EmployeeID, @IsTraining)";
                        SqlCommand cmdEmployeeRecap = new SqlCommand(cmdTextEmployeeRecap, conn);
                        cmdEmployeeRecap.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdEmployeeRecap.Parameters.AddWithValue("@EmployeeID", empId);
                        cmdEmployeeRecap.Parameters.AddWithValue("@IsTraining", isTraining);
                        cmdEmployeeRecap.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var Wsegments = NewRecap.WorkSegments.Where(s =>
                        (s.WorkStartDate.HasValue && s.WorkEndDate.HasValue && s.WorkStart.HasValue && s.WorkEnd.HasValue));

                    foreach (var seg in Wsegments)
                    {
                        const string sql = @"
                        INSERT INTO StartEndWork
                        (RecapID,
                        StartWorkTime, EndWorkTime, StartWorkDate, EndWorkDate)
                        VALUES
                        (@RecapID,
                        @StartWorkTime, @EndWorkTime, @StartWorkDate, @EndWorkDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@RecapID", RecapID);

                        cmd.Parameters.AddWithValue("@StartWorkTime", seg.WorkStart);
                        cmd.Parameters.AddWithValue("@EndWorkTime", seg.WorkEnd);
                        cmd.Parameters.AddWithValue("@StartWorkDate", seg.WorkStartDate);
                        cmd.Parameters.AddWithValue("@EndWorkDate", seg.WorkEndDate);
                        cmd.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var Rsegments = NewRecap.RecapSegments.Where(s =>
                        (s.RecapStartDate.HasValue && s.RecapEndDate.HasValue && s.RecapStart.HasValue && s.RecapEnd.HasValue));

                    foreach (var seg in Rsegments)
                    {
                        const string sql = @"
                            INSERT INTO StartEndRecap
                            (RecapID,
                            StartRecapTime, EndRecapTime, StartRecapDate, EndRecapDate)
                            VALUES
                            (@RecapID,
                            @StartRecapTime, @EndRecapTime, @StartRecapDate, @EndRecapDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@RecapID", RecapID);

                        cmd.Parameters.AddWithValue("@StartRecapTime", seg.RecapStart);
                        cmd.Parameters.AddWithValue("@EndRecapTime", seg.RecapEnd);
                        cmd.Parameters.AddWithValue("@StartRecapDate", seg.RecapStartDate);
                        cmd.Parameters.AddWithValue("@EndRecapDate", seg.RecapEndDate);
                        cmd.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var Tsegments = NewRecap.TravelSegments.Where(s =>
                        (s.DriveStartDate.HasValue && s.DriveEndDate.HasValue && s.DriveStart.HasValue && s.DriveEnd.HasValue));

                    foreach (var seg in Tsegments)
                    {
                        const string sql = @"
                            INSERT INTO StartEndTravel
                            (RecapID,
                            StartTravelTime, EndTravelTime, StartTravelDate, EndTravelDate)
                            VALUES
                            (@RecapID,
                            @StartTravelTime, @EndTravelTime, @StartTravelDate, @EndTravelDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@RecapID", RecapID);

                        cmd.Parameters.AddWithValue("@StartTravelTime", seg.DriveStart);
                        cmd.Parameters.AddWithValue("@EndTravelTime", seg.DriveEnd);
                        cmd.Parameters.AddWithValue("@StartTravelDate", seg.DriveStartDate);
                        cmd.Parameters.AddWithValue("@EndTravelDate", seg.DriveEndDate);

                        cmd.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var Lsegments = NewRecap.LunchSegments.Where(s =>
                        (s.LunchStartDate.HasValue && s.LunchEndDate.HasValue && s.LunchStart.HasValue && s.LunchEnd.HasValue));


                    foreach (var seg in Lsegments)
                    {

                        const string sql = @"
                            INSERT INTO StartEndLunch
                            (RecapID,
                            StartLunchTime, EndLunchTime, StartLunchDate, EndLunchDate)
                            VALUES
                            (@RecapID,
                            @StartLunchTime, @EndLunchTime, @StartLunchDate, @EndLunchDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@RecapID", RecapID);

                        cmd.Parameters.AddWithValue("@StartLunchTime", seg.LunchStart);
                        cmd.Parameters.AddWithValue("@EndLunchTime", seg.LunchEnd );
                        cmd.Parameters.AddWithValue("@StartLunchDate", seg.LunchStartDate);
                        cmd.Parameters.AddWithValue("@EndLunchDate", seg.LunchEndDate);
                        cmd.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var Ssegments = NewRecap.SupportSegments.Where(s =>
                        (s.SupportStartDate.HasValue && s.SupportEndDate.HasValue && s.SupportStart.HasValue && s.SupportEnd.HasValue));

                    foreach (var seg in Ssegments)
                    {
                        const string sql = @"
                            INSERT INTO StartEndSupport
                            (RecapID,
                            StartSupportTime, EndSupportTime, StartSupportDate, EndSupportDate)
                            VALUES
                            (@RecapID,
                            @StartSupportTime, @EndSupportTime, @StartSupportDate, @EndSupportDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@RecapID", RecapID);
                        cmd.Parameters.AddWithValue("@StartSupportTime", seg.SupportStart);
                        cmd.Parameters.AddWithValue("@EndSupportTime", seg.SupportEnd);
                        cmd.Parameters.AddWithValue("@StartSupportDate", seg.SupportStartDate);
                        cmd.Parameters.AddWithValue("@EndSupportDate", seg.SupportEndDate);
                        cmd.ExecuteNonQuery();
                    }

                    /* ============================Cat 6===================================== */
                    bool anyCat6Entered =
                        CableCat6.Cat6Start.HasValue && CableCat6.Cat6End.HasValue;

                    if (anyCat6Entered)
                    {
                        const string sqlCable = @"
                            INSERT INTO StartEndCat6
                            (RecapID,
                             StartCat6, EndCat6)
                            VALUES
                            (@RecapID,
                             @StartCat6, @EndCat6);";

                        SqlCommand cmdCable = new SqlCommand(sqlCable, conn);
                        cmdCable.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdCable.Parameters.AddWithValue("@StartCat6", CableCat6.Cat6Start);
                        cmdCable.Parameters.AddWithValue("@EndCat6", CableCat6.Cat6End);
                        cmdCable.ExecuteNonQuery();
                    }
                    /* ============================End of Cat 6===================================== */

                    /* ============================18/2===================================== */
                    bool any182Entered =
                        Cable182.Cable182Start.HasValue && Cable182.Cable182End.HasValue;

                    if (any182Entered)
                    {
                        const string sqlCable = @"
                            INSERT INTO StartEnd182
                            (RecapID,
                             Start182, End182)
                            VALUES
                            (@RecapID,
                             @Start182, @End182);";

                        SqlCommand cmdCable = new SqlCommand(sqlCable, conn);
                        cmdCable.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdCable.Parameters.AddWithValue("@Start182", Cable182.Cable182Start);
                        cmdCable.Parameters.AddWithValue("@End182", Cable182.Cable182End);
                        cmdCable.ExecuteNonQuery();
                    }
                    /* ============================End of 18/2===================================== */

                    /* ============================18/4===================================== */
                    bool any184Entered =
                        Cable184.Cable184Start.HasValue && Cable184.Cable184End.HasValue;

                    if (any184Entered)
                    {
                        const string sqlCable = @"
                            INSERT INTO StartEnd184
                            (RecapID,
                             Start184, End184)
                            VALUES
                            (@RecapID,
                             @Start184, @End184);";

                        SqlCommand cmdCable = new SqlCommand(sqlCable, conn);
                        cmdCable.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdCable.Parameters.AddWithValue("@Start184", Cable184.Cable184Start);
                        cmdCable.Parameters.AddWithValue("@End184", Cable184.Cable184End);
                        cmdCable.ExecuteNonQuery();
                    }
                    /* ============================End of 18/4===================================== */

                    /* ============================18/6===================================== */
                    bool any186Entered =
                        Cable186.Cable186Start.HasValue && Cable186.Cable186End.HasValue;

                    if (any186Entered)
                    {
                        const string sqlCable = @"
                            INSERT INTO StartEnd186
                            (RecapID,
                             Start186, End186)
                            VALUES
                            (@RecapID,
                             @Start186, @End186);";

                        SqlCommand cmdCable = new SqlCommand(sqlCable, conn);
                        cmdCable.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdCable.Parameters.AddWithValue("@Start186", Cable186.Cable186Start);
                        cmdCable.Parameters.AddWithValue("@End186", Cable186.Cable186End);
                        cmdCable.ExecuteNonQuery();
                    }
                    /* ============================End of 18/6===================================== */

                    /* ============================Fiber===================================== */
                    bool anyFiberEntered =
                        CableFiber.FiberStart.HasValue && CableFiber.FiberEnd.HasValue;

                    if (anyFiberEntered)
                    {
                        const string sqlCable = @"
                            INSERT INTO StartEndFiber
                            (RecapID,
                             StartFiber, EndFiber)
                            VALUES
                            (@RecapID,
                             @StartFiber, @EndFiber);";

                        SqlCommand cmdCable = new SqlCommand(sqlCable, conn);
                        cmdCable.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdCable.Parameters.AddWithValue("@StartFiber", CableFiber.FiberStart);
                        cmdCable.Parameters.AddWithValue("@EndFiber", CableFiber.FiberEnd);
                        cmdCable.ExecuteNonQuery();
                    }
                    /* ============================End of Fiber===================================== */

                    /* ============================Coax===================================== */
                    bool anyCoaxEntered =
                        CableCoax.CoaxStart.HasValue && CableCoax.CoaxEnd.HasValue;

                    if (anyCoaxEntered)
                    {
                        const string sqlCable = @"
                            INSERT INTO StartEndCoax
                            (RecapID,
                             StartCoax, EndCoax)
                            VALUES
                            (@RecapID,
                             @StartCoax, @EndCoax);";

                        SqlCommand cmdCable = new SqlCommand(sqlCable, conn);
                        cmdCable.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdCable.Parameters.AddWithValue("@StartCoax", CableCoax.CoaxStart);
                        cmdCable.Parameters.AddWithValue("@EndCoax", CableCoax.CoaxEnd);
                        cmdCable.ExecuteNonQuery();
                    }
                    /* ============================End of Coax===================================== */
                }
                return RedirectToPage("/AdminPages/BrowseRecaps");
            }
            else
            {
                OnGet();
                return Page();
            }
        }// End of 'OnPost'.

        private void PopulateVehicleList()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT * FROM Vehicle ORDER BY VehicleNumber";
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var vehicle = new SelectListItem()
                            {
                                Value = reader["VehicleID"].ToString(),
                                Text = $"{reader["VehicleNumber"]} ({reader["VehicleModel"]})"
                            };
                            Vehicles.Add(vehicle);
                        }
                    }
                }
            }
        }// End of 'PopulateVechileList'.

        private void PopulateEmployeeOptions()
        {
            EmployeeOptions = new List<SelectListItem>();

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            string sql = @"
               SELECT E.EmployeeID,
               E.EmployeeFName,
               E.EmployeeLName,
               SU.SystemUserRole
               FROM Employee AS E
               LEFT JOIN SystemUser AS SU
               ON E.EmployeeID = SU.EmployeeID;";

            using var cmd = new SqlCommand(sql, conn);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int employeeId = Convert.ToInt32(r["EmployeeID"]);

                string fName = Convert.ToString(r["EmployeeFName"]) ?? "";
                string lName = Convert.ToString(r["EmployeeLName"]) ?? "";


                string label = $"{fName} {lName}";

                EmployeeOptions.Add(new SelectListItem
                {
                    Value = employeeId.ToString(),
                    Text = label
                });
            }
        }// End of 'PopulateEmployeeOptions'.

        private static DateTime? CombineDateAndTime(DateTime? date, object time)
        {
            if (!date.HasValue || time == null) return null;

            if (time is DateTime dt)
                return new DateTime(date.Value.Year, date.Value.Month, date.Value.Day,
                                    dt.Hour, dt.Minute, dt.Second);

            if (time is TimeSpan ts)
                return new DateTime(date.Value.Year, date.Value.Month, date.Value.Day,
                                    ts.Hours, ts.Minutes, ts.Seconds);

            return null;
        }// End of 'CombineDateAndTime'.

        private static double Hours(DateTime? sd, object st, DateTime? ed, object et)
        {
            var start = CombineDateAndTime(sd, st);
            var end = CombineDateAndTime(ed, et);
            if (!start.HasValue || !end.HasValue) return 0.0;

            var span = end.Value - start.Value;
            return span.TotalHours < 0 ? 0.0 : span.TotalHours;
        }// End of 'Hours'.

        private bool SegmentDatesInvalid(DateTime? start, DateTime? end)
        {
            if (!start.HasValue || !end.HasValue)
                return false; // completeness rules handle missing values

            if (end.Value < start.Value)
                return true;

            var spanDays = (end.Value.Date - start.Value.Date).TotalDays;

            // Allow up to 2 calendar days: e.g., 11/01 -> 11/03 is OK
            return spanDays >= 2.0;
        }// End of 'SegmentDatesInvalid'.

        private static bool HasNonPositiveDuration(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            var start = CombineDateAndTime(sd, st);
            var end = CombineDateAndTime(ed, et);

            // If it's incomplete, let the completeness rules handle it
            if (!start.HasValue || !end.HasValue)
                return false;

            // Not allowed: end <= start (covers equal and “backwards”)
            return end.Value <= start.Value;
        }// End of 'HasNonPositiveDuration'.

        private static bool IsComplete(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            return sd.HasValue && st.HasValue && ed.HasValue && et.HasValue;
        }// End of 'IsComplete'.

        private static bool IsAnyFilled(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            return sd.HasValue || st.HasValue || ed.HasValue || et.HasValue;
        }// End of 'IsAnyFilled'.

        private static bool IsIncomplete(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            return IsAnyFilled(sd, st, ed, et) && !IsComplete(sd, st, ed, et);
        }// End of 'IsIncomplete'.

        private void ValidateCablePair(
            string modelPropertyName,  // e.g. "CableCat6"
            string startPropertyName,  // e.g. "Cat6Start"
            string endPropertyName,    // e.g. "Cat6End"
            int? start,
            int? end)
        {
            var keyStart = $"{modelPropertyName}.{startPropertyName}";
            var keyEnd = $"{modelPropertyName}.{endPropertyName}";

            bool hasStart = start.HasValue;
            bool hasEnd = end.HasValue;

            // One filled, one empty
            if (hasStart ^ hasEnd)
            {
                var msg = $"{modelPropertyName} start and end must both be provided or both left blank.";
                ModelState.AddModelError(keyStart, msg);
                ModelState.AddModelError(keyEnd, msg);
            }

            // Both filled, and end > start (using your original rule)
            if (hasStart && hasEnd && end.Value > start.Value)
            {
                var msg = $"{modelPropertyName} end footage cannot be greater than start footage.";
                ModelState.AddModelError(keyEnd, msg);
            }
        }// End of 'ValidateCablePair'.


        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();

                // If SystemUserRole is 2, set IsUserAdmin to true
                if (result != null && result.ToString() == "True")
                {
                    IsAdmin = true;
                    ViewData["IsAdmin"] = true;
                }
                else
                {
                    IsAdmin = false;
                }
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/

    }// End of 'AddRecap' Class.
}// End of 'namespace'.