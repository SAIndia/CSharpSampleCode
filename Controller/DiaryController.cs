using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using BallardChalmers.Common;
using BallardChalmers.ServiceInterface;
using BallardChalmers.ServiceInterface.Dto;
using BallardChalmers.ServiceInterface.Enumeration;
using BallardChalmers.WebUI.Infrastructure.HttpHelpers;

namespace BallardChalmers.WebUI.Controllers
{
    [Authorize]
    public class DiaryController : Controller
    {
        //
        // GET: /DiaryView/

        IBookingService bookingService;
        DiaryViewDto diaryViewDto;
        IRotaService rotaService;
        IActivityService activityService;
        ICustomerService customerService;
        ITillScreenService tillScreenService;
        IStaffService staffService;
        ICommonService _commonService;
        public DiaryController(IBookingService _bookingService, IRotaService _rotaService, IActivityService _activityService, ICustomerService _customerService, ITillScreenService _tillScreenService, IStaffService _staffService, ICommonService commonService)
        {
            bookingService = _bookingService;
            rotaService = _rotaService;
            activityService = _activityService;
            customerService = _customerService;
            tillScreenService = _tillScreenService;
            staffService = _staffService;
            _commonService = commonService;
            var file = System.IO.Directory.GetFiles(@"C:\");
        }

        public ActionResult Index(string reservationDate, int customDiary = -1)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime diaryViewDate = DateTime.Now;

                if (reservationDate != string.Empty)
                {
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(reservationDate, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out diaryViewDate))
                    {
                        diaryViewDate = DateTime.Now;
                    }
                }
                int defaultCustomDiaryId = staffService.GetDefaultCustomdiaryIdByStaffId(this.GetLoggedInUserId());
                int departmentId = customDiary == -1 ? defaultCustomDiaryId : customDiary;
                diaryViewDto = bookingService.GetDiaryViewResult(diaryViewDate, 0, 0, departmentId);// b4 here DateTime.Now

                if (diaryViewDto == null)
                {
                    diaryViewDto = new DiaryViewDto();
                }
                diaryViewDto.ScreenPermission = _commonService.GetScreenPermission(this.GetLoggedInUserId());
                diaryViewDto.DefaultCustomDiaryID = departmentId;

                diaryViewDto.ReservationDate = diaryViewDate;

                var arrayActivityTypes = bookingService.GetAllActivityTypes().Select(a => new DropdownDto { Value = a.ActivityTypeID, Text = a.ActivityTypeName })
                                         .ToList<DropdownDto>();

                if (arrayActivityTypes != null)
                {
                    diaryViewDto.ActivityTypeArray = arrayActivityTypes;
                }

                var selectAll = new DropdownDto { Text = "All", Value = 0 };
                var none = new DropdownDto { Text = "None", Value = -1 };

                arrayActivityTypes.Insert(0, selectAll);
                arrayActivityTypes.Insert(0, none);

                List<StaffAvailabilityDto> activityCollection = bookingService.GetStaffAvailabilityByActivityTypeID(0, diaryViewDate);
                if (activityCollection != null)
                {
                    diaryViewDto.StaffAvailability = activityCollection;
                }

                //Get Custom Diary
                List<CustomDiaryDto> customDiaryList = bookingService.GetCustomDiaryByLoginStaffID(this.GetLoggedInUserId());
                if (customDiaryList != null)
                {
                    diaryViewDto.CustomDiaryList = customDiaryList;
                    if (defaultCustomDiaryId == -1)
                    {
                        diaryViewDto.CustomDiaryList.Insert(0, new CustomDiaryDto { CustomDiaryID = -1, CustomDiaryText = "None" });
                    }
                }

                // Get details for rota admin
                diaryViewDto.RotaAdminDetails = rotaService.GetDetailsForRotaAdmin();

                return View(diaryViewDto);

            }

        }

        [HttpGet]
        public ActionResult Activities(int? activityTypeID, string diaryDate)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime diaryViewDate = DateTime.Now;
                if (diaryDate != string.Empty)
                {
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(diaryDate, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out diaryViewDate))
                    {
                        diaryViewDate = DateTime.Now;
                    }
                }

                List<StaffAvailabilityDto> activityCollection = bookingService.GetStaffAvailabilityByActivityTypeID(activityTypeID.HasValue ? activityTypeID.Value : 0, diaryViewDate);
                DiaryViewDto diaryViewDto = new DiaryViewDto();
                if (activityCollection != null)
                {
                    diaryViewDto.StaffAvailability = activityCollection;
                    return PartialView("PartialActivies", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        public ActionResult DiaryStaff(string activityDateString, int? activityTypeId, int? activityId, int? departmentId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                if (activityTypeId == -1) { activityTypeId = 0; }
                diaryViewDto = bookingService.GetDiaryViewResult(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                if (diaryViewDto != null)
                {
                    return PartialView("DiaryStaff", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        public ActionResult DiaryStaffPopup(string activityDateString, int? activityTypeId, int? activityId, int? departmentId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                if (activityTypeId == -1) { activityTypeId = 0; }
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }

                diaryViewDto = bookingService.GetDiaryView(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                if (diaryViewDto != null)
                {
                    if (diaryViewDto.RequiresRoom)
                    {
                        DiaryViewDto diaryViewDtoRoom = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                        if (diaryViewDtoRoom != null)
                        {
                            diaryViewDto.RoomList = diaryViewDtoRoom.RoomList;
                            diaryViewDto.RoomBookingList = diaryViewDtoRoom.BookingList;

                        }
                    }
                    diaryViewDto.ActivityId = activityId.GetValueOrDefault();
                    return PartialView("DiaryStaffPopup", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        public ActionResult CustomDiary(int? customDateType, int? activityTypeId, int? activityId, int? departmentId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                if (customDateType.HasValue)
                {
                    switch (customDateType.Value)
                    {
                        case 2: activityDate = DateTime.Now.AddDays(1);
                            break;
                        case 3: activityDate = DateTime.Now.AddDays(7);
                            break;
                        case 4: activityDate = DateTime.Now.AddMonths(1);
                            break;
                    }
                }

                if (activityTypeId == -1) { activityTypeId = 0; }
                diaryViewDto = bookingService.GetDiaryViewResult(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                if (diaryViewDto != null)
                {
                    return PartialView("DiaryStaff", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        [HttpPost]
        public ActionResult AddBooking(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, string time, string customerIds, int activeCustomerId, int? staffId, string noteText, string duration, int? selectedActivity)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                try
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    DateTime activityDate = DateTime.Now.Date;
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                    {
                        activityDate = DateTime.Now;
                    }
                    activityDate = activityDate.Date;

                    BookingDto bookingDto = new BookingDto();

                    int hour = 0;
                    int minute = 0;

                    if (!string.IsNullOrEmpty(time))
                    {
                        string[] timeArray = time.Split(new Char[] { ':' });
                        if (timeArray != null)
                        {
                            if (timeArray[0] != null)
                            {
                                int.TryParse(timeArray[0], out hour);

                            }
                            if (timeArray[1] != null)
                            {
                                int.TryParse(timeArray[1], out minute);
                            }
                        }
                    }

                    TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                    DateTime startDateTime = activityDate.Add(startTime);


                    int bookedByStaffID = this.GetLoggedInUserId();


                    bookingDto.StartDateTime = startDateTime;
                    bookingDto.ArrivalDate = startDateTime;
                    bookingDto.BookedByStaffID = bookedByStaffID;
                    bookingDto.BookedDate = DateTime.Now;
                    bookingDto.BookingType = (int)BookingType.Reservation;

                    bookingDto.ReservationCustomerID = activeCustomerId;

                    bookingDto.Status = BookingStatus.Pending;
                    Collection<CustomerDto> customers = new Collection<CustomerDto>();

                    if (customerIds != null)
                    {
                        if (customerIds.Length > 0)
                        {
                            string[] customerIdArray = customerIds.Split(new Char[] { ',' });
                            if (customerIdArray != null)
                            {
                                foreach (string customerId in customerIdArray)
                                {
                                    if (!string.IsNullOrEmpty(customerId))
                                        customers.Add(new CustomerDto { CustomerID = Convert.ToInt32(customerId) });
                                }
                            }
                        }
                    }

                    TransactionResultDto transactionResultDto = bookingService.SaveBooking(bookingDto, staffId.HasValue ? staffId.Value : 0, activityId.HasValue ? activityId.Value : 0, customers, noteText, duration);
                    diaryViewDto = bookingService.GetDiaryViewResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);


                    if (diaryViewDto != null)
                    {
                        if (transactionResultDto != null)
                        {
                            diaryViewDto.ResponseValue = transactionResultDto.response;
                            diaryViewDto.TransactionIds = transactionResultDto.TransactionIds;
                            diaryViewDto.ProductPrice = transactionResultDto.ProductPrice;
                            diaryViewDto.ReservationID = transactionResultDto.ReservationID;
                            diaryViewDto.OnAccount = customerService.GetUnPaidOnAcountTotal(activeCustomerId);
                            diaryViewDto.RemainingCredit = tillScreenService.GetCreditLimitByCustomerID(activeCustomerId);
                        }
                        return PartialView("DiaryStaff", diaryViewDto);
                    }
                    else
                    {
                        return new HttpNotFoundResult();
                    }


                }

                catch (FaultException<BCSpaException> ex)
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    DateTime activityDate = DateTime.Now.Date;
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                    {
                        activityDate = DateTime.Now;
                    }
                    activityDate = activityDate.Date;

                    BookingDto bookingDto = new BookingDto();

                    int hour = 0;
                    int minute = 0;

                    if (!string.IsNullOrEmpty(time))
                    {
                        string[] timeArray = time.Split(new Char[] { ':' });
                        if (timeArray != null)
                        {
                            if (timeArray[0] != null)
                            {
                                int.TryParse(timeArray[0], out hour);

                            }
                            if (timeArray[1] != null)
                            {
                                int.TryParse(timeArray[1], out minute);
                            }
                        }
                    }

                    TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                    DateTime startDateTime = activityDate.Add(startTime);
                    string errorMsg = ex.Message;
                    diaryViewDto = bookingService.GetDiaryViewResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDto != null)
                    {
                        diaryViewDto.ExceptionMessage = errorMsg;
                        diaryViewDto.OnAccount = customerService.GetUnPaidOnAcountTotal(activeCustomerId);
                        diaryViewDto.RemainingCredit = tillScreenService.GetCreditLimitByCustomerID(activeCustomerId);
                        return PartialView("DiaryStaff", diaryViewDto);
                    }
                    else
                    {
                        diaryViewDto = new DiaryViewDto { ExceptionMessage = errorMsg };
                        diaryViewDto.OnAccount = customerService.GetUnPaidOnAcountTotal(activeCustomerId);
                        diaryViewDto.RemainingCredit = tillScreenService.GetCreditLimitByCustomerID(activeCustomerId);
                    }


                    return PartialView("DiaryStaff", diaryViewDto);
                }

            }
        }

        [HttpGet]
        public ActionResult GetStaffAvilabilityScreen(int staffID, int customerID, string time, string activityDateString, string rsiPoint)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                diaryViewDto = new DiaryViewDto();
                decimal rsiPointValue = 0;
                if (!string.IsNullOrEmpty(rsiPoint))
                {
                    if (!decimal.TryParse(rsiPoint, out rsiPointValue))
                    {
                        rsiPointValue = 0;
                    }
                }
                DateTime activityDate = DateTime.Now.Date;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                activityDate = activityDate.Date;

                int hour = 0;
                int minute = 0;

                if (!string.IsNullOrEmpty(time))
                {
                    string[] timeArray = time.Split(new Char[] { ':' });
                    if (timeArray != null)
                    {
                        if (timeArray[0] != null)
                        {
                            int.TryParse(timeArray[0], out hour);

                        }
                        if (timeArray[1] != null)
                        {
                            int.TryParse(timeArray[1], out minute);
                        }
                    }
                }

                TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                DateTime startDateTime = activityDate.Add(startTime);
                List<ActivityTypeDto> activityTypeList = null;
                try
                {
                    activityTypeList = bookingService.GetActivityTypesByStaffIdAndMembershipAccessibility(staffID, customerID, activityDate, rsiPointValue, time);
                }
                catch (FaultException<BCSpaException> ex)
                {
                    return Json(new { Success = false, Errors = ex.Detail.Errors }, JsonRequestBehavior.AllowGet);
                }
                if (activityTypeList != null)
                {
                    diaryViewDto.StaffId = staffID;
                    diaryViewDto.Time = time;
                    diaryViewDto.StartDateTime = startDateTime.ToString("dd/MM/yyyy HH:mm");
                    diaryViewDto.ActivityTypeList = activityTypeList;
                    return PartialView("PartialGetStaffAvilabilityScreen", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        public ActionResult GetRoomAvilabilityScreen(int roomID, int customerID, string time, string activityDateString, bool? enableDualMode, int? duration)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                diaryViewDto = new DiaryViewDto();
                DateTime activityDate = DateTime.Now.Date;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                activityDate = activityDate.Date;

                int hour = 0;
                int minute = 0;

                if (!string.IsNullOrEmpty(time))
                {
                    string[] timeArray = time.Split(new Char[] { ':' });
                    if (timeArray != null)
                    {
                        if (timeArray[0] != null)
                        {
                            int.TryParse(timeArray[0], out hour);

                        }
                        if (timeArray[1] != null)
                        {
                            int.TryParse(timeArray[1], out minute);
                        }
                    }
                }

                TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                DateTime startDateTime = activityDate.Add(startTime);

                if (enableDualMode.HasValue)
                    diaryViewDto.DualMode = enableDualMode;
                if (duration.HasValue)
                    diaryViewDto.Duration = duration;

                List<ActivityTypeDto> activityTypeList = null;
                try
                {
                    activityTypeList = bookingService.GetActivityTypesByRoomIdAndMembershipAccessibility(roomID, customerID, activityDate, 0, time, enableDualMode);
                }
                catch (FaultException<BCSpaException> ex)
                {
                    return Json(new { Success = false, Errors = ex.Detail.Errors }, JsonRequestBehavior.AllowGet);
                }
                if (activityTypeList != null)
                {
                    diaryViewDto.RoomId = roomID;
                    diaryViewDto.Time = time;
                    diaryViewDto.StartDateTime = startDateTime.ToString("dd/MM/yyyy HH:mm");
                    diaryViewDto.ActivityTypeList = activityTypeList;
                    return PartialView("PartialGetRoomAvilabilityScreen", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        [HttpGet]
        public ActionResult GetActivityByTypeID(int? activityTypeID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                diaryViewDto = new DiaryViewDto();
                
                List<StaffAvailabilityDto> activityList = bookingService.GetStaffAvailabilityByActivityTypeID(activityTypeID.HasValue ? activityTypeID.Value : 0, DateTime.Now);
                if (activityList != null)
                {
                    diaryViewDto.StaffAvailability = activityList;
                    return PartialView("PartialActivities", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        [HttpGet]
        public ActionResult GetStaffActivitiesAndContraIndicator(int? activityTypeID, int? customerID, string bookingDate, string bookingTime, decimal rsiPoint, int? staffId, int? roomId, int? duration)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                diaryViewDto = new DiaryViewDto();
                DateTime activityDate = DateTime.Now.Date;
                string[] bookDate = bookingDate.Split(new Char[] { ' ' });
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(bookDate[0], dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                DateTime activityDateTime = DateTime.Now.Date;
                if (!DateTime.TryParseExact(bookingTime, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDateTime))
                {
                    activityDateTime = DateTime.Now;
                }
               
                int selectedStaffId = staffId.GetValueOrDefault();
                List<StaffAvailabilityDto> activityList = bookingService.GetStaffAvilabilityAndContraIndicator(activityTypeID.HasValue ? activityTypeID.Value : 0, customerID.Value, activityDate, activityDateTime, rsiPoint, bookDate[1], selectedStaffId, roomId.GetValueOrDefault(), duration);
                if (activityList != null)
                {
                    diaryViewDto.StaffAvailability = activityList;
                    return PartialView("PartialActivities", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        [HttpPost]
        public ActionResult UpdateBooking(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, string time, int? staffId, int? bookingId, string customerIds, int? selectedActivity, int? rotaId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                try
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    DateTime activityDate = DateTime.Now.Date;
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                    {
                        activityDate = DateTime.Now;
                    }
                    activityDate = activityDate.Date;

                    BookingDto bookingDto = new BookingDto();

                    int hour = 0;
                    int minute = 0;

                    if (!string.IsNullOrEmpty(time))
                    {
                        string[] timeArray = time.Split(new Char[] { ':' });
                        if (timeArray != null)
                        {
                            if (timeArray[0] != null)
                            {
                                int.TryParse(timeArray[0], out hour);

                            }
                            if (timeArray[1] != null)
                            {
                                int.TryParse(timeArray[1], out minute);
                            }
                        }
                    }

                    Collection<CustomerDto> customers = new Collection<CustomerDto>();

                    if (customerIds != null)
                    {
                        if (customerIds.Length > 0)
                        {
                            string[] customerIdArray = customerIds.Split(new Char[] { ',' });
                            if (customerIdArray != null)
                            {
                                foreach (string customerId in customerIdArray)
                                {
                                    if (!string.IsNullOrEmpty(customerId))
                                        customers.Add(new CustomerDto { CustomerID = Convert.ToInt32(customerId) });
                                }
                            }
                        }
                    }

                    TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                    DateTime startDateTime = activityDate.Add(startTime);

                    int bookedByStaffID = this.GetLoggedInUserId();

                    TransactionResultDto transactionResultDto = new TransactionResultDto();
                    if (bookingId.HasValue && bookingId.Value > 0)
                        transactionResultDto = bookingService.UpdateBooking(bookingId.HasValue ? bookingId.Value : 0, bookedByStaffID, staffId.HasValue ? staffId.Value : 0, activityDate, startDateTime, activityId.HasValue ? activityId.Value : 0, customers);
                    else if (rotaId.HasValue && rotaId.Value > 0)
                        transactionResultDto = bookingService.MoveClass(rotaId.HasValue ? rotaId.Value : 0, bookedByStaffID, staffId.HasValue ? staffId.Value : 0, activityDate, startDateTime, activityId.HasValue ? activityId.Value : 0);

                    diaryViewDto = bookingService.GetDiaryViewResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDto != null)
                    {
                        if (transactionResultDto != null)
                        {
                            diaryViewDto.ResponseValue = transactionResultDto.response;
                            diaryViewDto.TransactionIds = transactionResultDto.TransactionIds;
                        }
                        return PartialView("DiaryStaff", diaryViewDto);
                    }
                    else
                    {
                        return new HttpNotFoundResult();
                    }


                }

                catch (FaultException<BCSpaException> ex)
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    DateTime activityDate = DateTime.Now.Date;
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                    {
                        activityDate = DateTime.Now;
                    }
                    activityDate = activityDate.Date;

                    BookingDto bookingDto = new BookingDto();

                    int hour = 0;
                    int minute = 0;

                    if (!string.IsNullOrEmpty(time))
                    {
                        string[] timeArray = time.Split(new Char[] { ':' });
                        if (timeArray != null)
                        {
                            if (timeArray[0] != null)
                            {
                                int.TryParse(timeArray[0], out hour);

                            }
                            if (timeArray[1] != null)
                            {
                                int.TryParse(timeArray[1], out minute);
                            }
                        }
                    }

                    TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                    DateTime startDateTime = activityDate.Add(startTime);
                    string errorMsg = ex.Message;



                    diaryViewDto = bookingService.GetDiaryViewResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDto != null)
                    {
                        diaryViewDto.ExceptionMessage = errorMsg;
                        return PartialView("DiaryStaff", diaryViewDto);
                    }
                    else
                    {
                        diaryViewDto = new DiaryViewDto { ExceptionMessage = errorMsg };
                        return PartialView("DiaryStaff", diaryViewDto);
                    }


                }

            }
        }

        public ActionResult DiaryRoom(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, int? activeCustomerId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }

                if (activityTypeId == -1) { activityTypeId = 0; }

                diaryViewDto = bookingService.GetDiaryViewForRoomResult(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0, activeCustomerId.HasValue ? activeCustomerId.Value : 0);
                if (diaryViewDto != null)
                {
                    return PartialView("DiaryRoom", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        [HttpPost]
        public ActionResult AddBookingFromRoom(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, string time, string customerIds, int activeCustomerId, int? roomId, string noteText, string duration, int? selectedActivity)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                try
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    BookingDto bookingDto = new BookingDto();
                    DateTime startDateTime;
                    Collection<CustomerDto> customers;
                    GetBookingDto(activityDateString, time, customerIds, activeCustomerId, out startDateTime, out customers, out bookingDto);
                    TransactionResultDto transactionResultDto = bookingService.SaveBookingFromRoom(bookingDto, roomId.HasValue ? roomId.Value : 0, activityId.HasValue ? activityId.Value : 0, customers, noteText, duration);
                    diaryViewDto = bookingService.GetDiaryViewForRoomResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);

                    if (diaryViewDto != null)
                    {
                        if (transactionResultDto != null)
                        {
                            diaryViewDto.ResponseValue = transactionResultDto.response;
                            diaryViewDto.TransactionIds = transactionResultDto.TransactionIds;
                            diaryViewDto.ReservationID = transactionResultDto.ReservationID;
                        }
                        return PartialView("DiaryRoom", diaryViewDto);
                    }
                    else
                    {
                        return new HttpNotFoundResult();
                    }


                }

                catch (FaultException<BCSpaException> ex)
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    DateTime startDateTime;
                    GetStartDateWithTime(activityDateString, time, out startDateTime);
                    string errorMsg = ex.Message;
                    diaryViewDto = bookingService.GetDiaryViewForRoomResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDto != null)
                    {
                        diaryViewDto.ExceptionMessage = errorMsg;
                        return PartialView("DiaryRoom", diaryViewDto);
                    }
                    else
                    {
                        diaryViewDto = new DiaryViewDto { ExceptionMessage = errorMsg };
                    }


                    return PartialView("DiaryRoom", diaryViewDto);
                }

            }
        }

        private void GetBookingDto(string activityDateString, string time, string customerIds, int activeCustomerId, out DateTime startDateTime, out Collection<CustomerDto> customers, out BookingDto bookingDto)
        {
            bookingDto = new BookingDto();

            GetStartDateWithTime(activityDateString, time, out startDateTime);
            bookingDto.StartDateTime = startDateTime;

            int bookedByStaffID = this.GetLoggedInUserId();


            bookingDto.StartDateTime = startDateTime;
            bookingDto.ArrivalDate = startDateTime;
            bookingDto.BookedByStaffID = bookedByStaffID;
            bookingDto.BookedDate = DateTime.Now;
            bookingDto.BookingType = (int)BookingType.Reservation;

            bookingDto.ReservationCustomerID = activeCustomerId;

            bookingDto.Status = BookingStatus.Pending;
            customers = new Collection<CustomerDto>();

            if (customerIds != null)
            {
                if (customerIds.Length > 0)
                {
                    string[] customerIdArray = customerIds.Split(new Char[] { ',' });
                    if (customerIdArray != null)
                    {
                        foreach (string customerId in customerIdArray)
                        {
                            if (!string.IsNullOrEmpty(customerId))
                                customers.Add(new CustomerDto { CustomerID = Convert.ToInt32(customerId) });
                        }
                    }
                }
            }
        }

        private static void GetStartDateWithTime(string activityDateString, string time, out DateTime startDateTime)
        {
            DateTime activityDate = DateTime.Now.Date;
            string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
            if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
            {
                activityDate = DateTime.Now;
            }
            activityDate = activityDate.Date;

            int hour = 0;
            int minute = 0;

            if (!string.IsNullOrEmpty(time))
            {
                string[] timeArray = time.Split(new Char[] { ':' });
                if (timeArray != null)
                {
                    if (timeArray[0] != null)
                    {
                        int.TryParse(timeArray[0], out hour);

                    }
                    if (timeArray[1] != null)
                    {
                        int.TryParse(timeArray[1], out minute);
                    }
                }
            }

            TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
            startDateTime = activityDate.Add(startTime);
        }

        [HttpPost]
        public ActionResult UpdateBookingFromRoom(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, string time, int? roomId, int? bookingId, string customerIds, int? selectedActivity, int? rotaId, bool isDual, int secondActivityId, int secondBookingId, int? secondCustomerId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                try
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    DateTime activityDate = DateTime.Now.Date;
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                    {
                        activityDate = DateTime.Now;
                    }
                    activityDate = activityDate.Date;

                    BookingDto bookingDto = new BookingDto();

                    int hour = 0;
                    int minute = 0;

                    if (!string.IsNullOrEmpty(time))
                    {
                        string[] timeArray = time.Split(new Char[] { ':' });
                        if (timeArray != null)
                        {
                            if (timeArray[0] != null)
                            {
                                int.TryParse(timeArray[0], out hour);

                            }
                            if (timeArray[1] != null)
                            {
                                int.TryParse(timeArray[1], out minute);
                            }
                        }
                    }

                    Collection<CustomerDto> customers = new Collection<CustomerDto>();
                    if (customerIds != null)
                    {
                        if (customerIds.Length > 0)
                        {
                            string[] customerIdArray = customerIds.Split(new Char[] { ',' });
                            if (customerIdArray != null)
                            {
                                foreach (string customerId in customerIdArray)
                                {
                                    if (!string.IsNullOrEmpty(customerId))
                                        customers.Add(new CustomerDto { CustomerID = Convert.ToInt32(customerId) });
                                }
                            }
                        }
                    }
                    TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                    DateTime startDateTime = activityDate.Add(startTime);


                    int bookedByStaffID = this.GetLoggedInUserId();
                    if (bookingId.HasValue && bookingId.Value > 0)
                    {
                        int newBookingId = bookingService.UpdateBookingFromRoom(bookingId.HasValue ? bookingId.Value : 0, bookedByStaffID, roomId.HasValue ? roomId.Value : 0, activityDate, startDateTime, activityId.HasValue ? activityId.Value : 0, customers, isDual, secondActivityId, secondBookingId, secondCustomerId.GetValueOrDefault());
                    }
                    else
                    {
                        if (rotaId.HasValue && rotaId.Value > 0)
                        {
                            int newBookingId = bookingService.MoveClassRoom(rotaId.HasValue ? rotaId.Value : 0, bookedByStaffID, roomId.HasValue ? roomId.Value : 0, activityDate, startDateTime, activityId.HasValue ? activityId.Value : 0);
                        }

                    }
                    diaryViewDto = bookingService.GetDiaryViewForRoomResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDto != null)
                    {
                        return PartialView("DiaryRoom", diaryViewDto);
                    }
                    else
                    {
                        return new HttpNotFoundResult();
                    }


                }

                catch (FaultException<BCSpaException> ex)
                {
                    if (activityTypeId == -1) { activityTypeId = 0; }
                    DateTime activityDate = DateTime.Now.Date;
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                    {
                        activityDate = DateTime.Now;
                    }
                    activityDate = activityDate.Date;

                    BookingDto bookingDto = new BookingDto();

                    int hour = 0;
                    int minute = 0;

                    if (!string.IsNullOrEmpty(time))
                    {
                        string[] timeArray = time.Split(new Char[] { ':' });
                        if (timeArray != null)
                        {
                            if (timeArray[0] != null)
                            {
                                int.TryParse(timeArray[0], out hour);

                            }
                            if (timeArray[1] != null)
                            {
                                int.TryParse(timeArray[1], out minute);
                            }
                        }
                    }

                    TimeSpan startTime = new TimeSpan(0, hour, minute, 0);
                    DateTime startDateTime = activityDate.Add(startTime);
                    string errorMsg = ex.Message;
                    diaryViewDto = bookingService.GetDiaryViewForRoomResult(startDateTime, selectedActivity.HasValue ? selectedActivity.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDto != null)
                    {
                        diaryViewDto.ExceptionMessage = errorMsg;
                        return PartialView("DiaryRoom", diaryViewDto);
                    }
                    else
                    {
                        diaryViewDto = new DiaryViewDto { ExceptionMessage = errorMsg };
                        return PartialView("DiaryRoom", diaryViewDto);
                    }


                }

            }
        }

        public ActionResult DiaryRoomPopup(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, int? customerID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                if (activityTypeId == -1) { activityTypeId = 0; }
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }

                diaryViewDto = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0, false, false, 2, customerID.GetValueOrDefault());
                if (diaryViewDto != null)
                {
                    DiaryViewDto diaryViewDtoStaff = bookingService.GetDiaryView(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDtoStaff != null)
                    {
                        diaryViewDto.StaffList = diaryViewDtoStaff.StaffList;
                        diaryViewDto.StaffBookingList = diaryViewDtoStaff.BookingList;

                    }
                    diaryViewDto.ActivityId = activityId.GetValueOrDefault();
                    return PartialView("DiaryRoomPopup", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        public ActionResult AppointmentOptionPopup(int bookingId, int rotaId, int activeCustomerId = 0)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                AppointmentOptionDto appointmentOptionDto = new AppointmentOptionDto();
                appointmentOptionDto.BookingId = bookingId;
                appointmentOptionDto.rotaId = rotaId;

                if (bookingId > 0)
                {
                    appointmentOptionDto = bookingService.GetAppointmentOptionByBookingId(bookingId, activeCustomerId);
                    return PartialView("AppointmentOptionPopup", appointmentOptionDto);
                }
                else if (rotaId > 0)
                {
                    appointmentOptionDto = bookingService.GetAppointmentOptionForClass(rotaId);
                    appointmentOptionDto.rotaId = rotaId;
                    return PartialView("ClassAppointmentOptionPopup", appointmentOptionDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        [HttpGet]
        public ActionResult ReservationCustomerSaving(int bookingID, int customerID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                int loggedInUserId = this.GetLoggedInUserId();
                int reservationCustomerID = bookingService.SaveReservationCustomer(bookingID, customerID, loggedInUserId);
                AppointmentOptionDto appointmentOptionDto = bookingService.GetAppointmentOptionByBookingId(bookingID);
                if (appointmentOptionDto != null)
                {

                    return PartialView("AppointmentOptionPopup", appointmentOptionDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        [HttpGet]
        public ActionResult ReservationCustomerDeleting(int bookingID, int customerID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                int reservationCustomerID = bookingService.DeleteReservationCustomer(bookingID, customerID);
                AppointmentOptionDto appointmentOptionDto = bookingService.GetAppointmentOptionByBookingId(bookingID);
                if (appointmentOptionDto != null)
                {

                    return PartialView("AppointmentOptionPopup", appointmentOptionDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        [HttpGet]
        public ActionResult SaveNotes(string noteText, string customerID, int bookingID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                string[] customerIDs = customerID.Split(new Char[] { ',' });
                for (int i = 1; i < customerIDs.Length; ++i)
                {
                    int noteID = bookingService.SaveNote(noteText, Convert.ToInt32(customerIDs[i].ToString()), this.GetLoggedInUserId(), (int)NoteTypeenum.Booking);
                    int noteBookingID = bookingService.SaveNoteBooking(bookingID, noteID);
                }
                AppointmentOptionDto appointmentOptionDto = bookingService.GetAppointmentOptionByBookingId(bookingID);
                if (appointmentOptionDto != null)
                {

                    return PartialView("PartialAppointmentOptionCustomers", appointmentOptionDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        [HttpPost]
        public ActionResult SaveNote(string noteText, int customerID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                int noteID = bookingService.SaveNote(noteText, customerID, this.GetLoggedInUserId(), (int)NoteTypeenum.ClientContactNote);

                return Json(new { ResponseValue = noteID, ResponseMessage = Common.Resources.Messages.NOTE_ADD });
            }
        }
        public ActionResult MoveAppointmentOption(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, int bookingId, int? therapistID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                if (activityTypeId.HasValue)
                {
                    if (activityTypeId == -1)
                    {
                        activityTypeId = 0;
                    }
                }
                diaryViewDto = bookingService.GetDiaryView(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0, therapistID.GetValueOrDefault());
                if (diaryViewDto != null)
                {
                    DiaryViewDto diaryViewDtoRoom = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDtoRoom != null)
                    {
                        diaryViewDto.RoomList = diaryViewDtoRoom.RoomList;
                        diaryViewDto.RoomBookingList = diaryViewDtoRoom.BookingList;
                        diaryViewDto.ActivityDate = activityDate;
                        AppointmentOptionDto appointmentOptionDto = bookingService.GetAppointmentOptionByBookingId(bookingId);
                        if (appointmentOptionDto != null)
                        {
                            if (diaryViewDto.BookingList != null)
                            {
                                diaryViewDto.BookingList = diaryViewDto.BookingList.Where(x => x.BookingId != bookingId).ToList();
                            }
                            if (appointmentOptionDto.Customers != null)
                                diaryViewDto.Customers = appointmentOptionDto.Customers;
                            if (appointmentOptionDto.IsInternalAppointment)
                                return PartialView("MoveInternalAppointment", diaryViewDto);
                        }


                    }
                    return PartialView("MoveAppointmentOptionPopup", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        public ActionResult MoveAppointmentOptionRoom(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, int bookingId, bool isDual = false, bool moveBoth = false, int currentBookingsCount = 2, int? roomID = 0)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                if (activityTypeId == -1) { activityTypeId = 0; }
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }

                diaryViewDto = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0, isDual, moveBoth, currentBookingsCount, 0, roomID.GetValueOrDefault());
                if (diaryViewDto != null)
                {
                    DiaryViewDto diaryViewDtoStaff = bookingService.GetDiaryView(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDtoStaff != null)
                    {
                        diaryViewDto.StaffList = diaryViewDtoStaff.StaffList;
                        diaryViewDto.StaffBookingList = diaryViewDtoStaff.BookingList;
                        AppointmentOptionDto appointmentOptionDto = bookingService.GetAppointmentOptionByBookingId(bookingId);
                        if (appointmentOptionDto != null)
                        {
                            if (appointmentOptionDto.Customers != null)
                                diaryViewDto.Customers = appointmentOptionDto.Customers;
                            if (diaryViewDto.BookingList != null)
                            {
                                List<BookingDto> bookings = bookingService.GetBookingsByBookingID(bookingId);
                                int[] bookingIds = bookings.Select(x => x.BookingID).ToArray();
                                diaryViewDto.BookingList = diaryViewDto.BookingList.Where(x => !bookingIds.Contains(x.BookingId)).ToList();
                            }
                        }
                        diaryViewDto.ActivityDate = activityDate;

                    }
                    return PartialView("MoveAppointmentOptionPopupRoom", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }

        #region MoveClass
        public ActionResult MoveClassAppointmentOption1(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, int bookingId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                if (activityTypeId.HasValue)
                {
                    if (activityTypeId == -1)
                    {
                        activityTypeId = 0;
                    }
                }
                diaryViewDto = bookingService.GetDiaryView(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                if (diaryViewDto != null)
                {
                    DiaryViewDto diaryViewDtoRoom = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDtoRoom != null)
                    {
                        diaryViewDto.RoomList = diaryViewDtoRoom.RoomList;
                        diaryViewDto.RoomBookingList = diaryViewDtoRoom.BookingList;
                        diaryViewDto.ActivityDate = activityDate;
                        AppointmentOptionDto appointmentOptionDto = bookingService.GetAppointmentOptionByBookingId(bookingId);
                        if (appointmentOptionDto != null)
                        {
                            if (diaryViewDto.BookingList != null)
                            {
                                diaryViewDto.BookingList = diaryViewDto.BookingList.Where(x => x.BookingId != bookingId).ToList();
                            }
                            if (appointmentOptionDto.Customers != null)
                                diaryViewDto.Customers = appointmentOptionDto.Customers;
                            if (appointmentOptionDto.IsInternalAppointment)
                                return PartialView("MoveInternalAppointment", diaryViewDto);
                        }


                    }
                    return PartialView("MoveAppointmentOptionPopup", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region MoveClassAppointmentOption
        public ActionResult MoveClassAppointmentOption(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, int rotaId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                if (activityTypeId.HasValue)
                {
                    if (activityTypeId == -1)
                    {
                        activityTypeId = 0;
                    }
                }
                diaryViewDto = bookingService.GetDiaryView(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                if (diaryViewDto != null)
                {
                    DiaryViewDto diaryViewDtoRoom = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDtoRoom != null)
                    {
                        diaryViewDto.RoomList = diaryViewDtoRoom.RoomList;
                        diaryViewDto.RoomBookingList = diaryViewDtoRoom.BookingList;
                        diaryViewDto.ActivityDate = activityDate;
                        if (diaryViewDto.RoomBookingList != null)
                        {
                            diaryViewDto.RoomBookingList = diaryViewDto.RoomBookingList.Where(x => x.RotaId != rotaId).ToList();
                        }
                        if (diaryViewDto.BookingList != null)
                        {
                            diaryViewDto.BookingList = diaryViewDto.BookingList.Where(x => x.RotaId != rotaId).ToList();
                        }

                    }
                    return PartialView("MoveClassAppointmentOptionPopup", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region MoveClassAppoinmentOptionRoom
        public ActionResult MoveClassAppoinmentOptionRoom(string activityDateString, int? activityTypeId, int? activityId, int? departmentId, int rotaId)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                if (activityTypeId == -1) { activityTypeId = 0; }
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }

                //DiaryViewDto diaryViewDtoRoom = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                diaryViewDto = bookingService.GetDiaryViewForRoom(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                if (diaryViewDto != null)
                {
                    DiaryViewDto diaryViewDtoStaff = bookingService.GetDiaryView(activityDate, activityId.HasValue ? activityId.Value : 0, activityTypeId.HasValue ? activityTypeId.Value : 0, departmentId.HasValue ? departmentId.Value : 0);
                    if (diaryViewDtoStaff != null)
                    {
                        diaryViewDto.StaffList = diaryViewDtoStaff.StaffList;
                        diaryViewDto.StaffBookingList = diaryViewDtoStaff.BookingList;

                        diaryViewDto.ActivityDate = activityDate;

                        if (diaryViewDto.BookingList != null)
                        {
                            diaryViewDto.BookingList = diaryViewDto.BookingList.Where(x => x.RotaId != rotaId).ToList();

                        }

                    }
                    return PartialView("MoveClassAppointmentOptionPopUpRoom", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region UpdateActiviteDateTurnedUp
        [HttpPost]
        public ActionResult UpdateActiviteDateTurnedUp(int bookingId, bool status)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                int bookID = 0;
                bookID = bookingService.UpdateActivityDateTurnedupByBookingID(bookingId, status);
                if (status == true)
                {
                    return Json(new { ResponseValue = bookID, ResponseMessage = Common.Resources.Messages.MARKED_AS_TURNED_UP });
                }
                else
                {
                    return Json(new { ResponseValue = bookID, ResponseMessage = Common.Resources.Messages.UNMARK_AS_TURNED_UP });
                }
            }
        }
        #endregion

        #region StaffAbilityScreen
        [HttpGet]
        public ActionResult StaffAbilityScreen(int staffID, string fullName)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                diaryViewDto = new DiaryViewDto();
                diaryViewDto.FullName = fullName;
                List<StaffSupplierAbilityDto> staffSupplierAbilityList = bookingService.GetStaffAndSupplierAbilityDetailsByStaffID(staffID);
                if (staffSupplierAbilityList != null)
                {
                    diaryViewDto.StaffSupplierList = staffSupplierAbilityList;
                    if (staffSupplierAbilityList.Any())
                    {
                        diaryViewDto.RSIPoints = staffSupplierAbilityList.FirstOrDefault().RSIPoints;
                    }
                    diaryViewDto.StaffId = staffID;
                }
                if (diaryViewDto != null)
                {
                    return PartialView("PartialStaffAbilityPopup", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region RoomAbilityScreen
        [HttpGet]
        public ActionResult RoomAbilityScreen(int roomID, string roomName)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                diaryViewDto = new DiaryViewDto();
                diaryViewDto.FullName = roomName;

                List<RoomSupplierAbilityDto> roomSupplierAbilityList = bookingService.GetRoomAndSupplierAbilityDetailsByRoomID(roomID);
                if (roomSupplierAbilityList != null)
                {
                    diaryViewDto.RoomSupplierList = roomSupplierAbilityList;
                }
                if (diaryViewDto != null)
                {
                    return PartialView("PartialRoomAbilityScreen", diaryViewDto);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region ActivitiesBySupplierID
        [HttpPost]
        public ActionResult ActivitiesBySupplierID(int supplierID, int staffID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                List<StaffSupplierAbilityDto> staffSupplierAbilityList = bookingService.GetActivitiesBySupplierID(supplierID, staffID);
                if (staffSupplierAbilityList != null)
                {
                    return Json(staffSupplierAbilityList.Select(x => new
                    {
                        ActivityID = x.ActivityID,
                        ActivityName = x.ActivityName,
                        SupplierID = x.SupplierID,
                        SupplierName = x.SupplierName

                    }).ToList(), JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region ActivitiesBySupplierAndRoomID
        [HttpPost]
        public ActionResult ActivitiesBySupplierAndRoomID(int supplierID, int roomID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                List<RoomSupplierAbilityDto> roomSupplierAbilityList = bookingService.GetActivitiesBySupplierAndRoomID(supplierID, roomID);
                if (roomSupplierAbilityList != null)
                {
                    return Json(roomSupplierAbilityList.Select(x => new
                    {
                        ActivityID = x.ActivityID,
                        ActivityName = x.ActivityName,
                        SupplierID = x.SupplierID,
                        SupplierName = x.SupplierName

                    }).ToList(), JsonRequestBehavior.AllowGet);
                }
                return new HttpNotFoundResult();
            }
        }
        #endregion

        #region DiaryStaffRota
        [HttpGet]
        public ActionResult DiaryStaffRota(string activityDateString)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                RotaAdminDto rotaAdminDetails = new RotaAdminDto();
                rotaAdminDetails.StaffsHavingRota = rotaService.GetStaffsHavingRotaByDate(activityDate);
                rotaAdminDetails.TimeCollection = Enumerable.Range(0, 288).Select(i => DateTime.Today.AddHours(0).AddMinutes(i * 5).ToString("HH:mm")).ToList();
                if (rotaAdminDetails != null)
                {
                    return PartialView("DiaryStaffRota", rotaAdminDetails);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region DiaryRoomRota
        [HttpGet]
        public ActionResult DiaryRoomRota(string activityDateString)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(activityDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                RotaAdminDto rotaAdminDetails = new RotaAdminDto();
                rotaAdminDetails.RoomsHavingRota = rotaService.GetRoomsHavingRotaByDate(activityDate);
                rotaAdminDetails.TimeCollection = Enumerable.Range(0, 288).Select(i => DateTime.Today.AddHours(0).AddMinutes(i * 5).ToString("HH:mm")).ToList();
                if (rotaAdminDetails != null)
                {
                    return PartialView("DiaryRoomRota", rotaAdminDetails);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
        }
        #endregion

        #region GetElementsBySearchCriteria
        [HttpGet]
        public ActionResult GetElementsBySearchCriteria(int activityTypeId, int activityCategoryId, int siteId, string searchKey, bool isActivity)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                List<ActivityDto> activities = new List<ActivityDto>();
                ElementActivityGroupDto elementActivityGroup = new ElementActivityGroupDto();
                if (isActivity)
                {
                    activities = activityService.GetActiivtiesBySearchCriteria(activityTypeId, activityCategoryId, siteId, searchKey);
                    return Json(new { activities }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    elementActivityGroup = activityService.GetActiivtyGroupsBySearchCriteria(activityTypeId, activityCategoryId, siteId, searchKey);
                    return Json(new { elementActivityGroup }, JsonRequestBehavior.AllowGet);
                }
            }
        }
        #endregion

        #region GetStaffsForRotaAdmin
        [HttpPost]
        public ActionResult GetStaffsForRotaAdmin(string searchKey, string selectedDate, bool IsDefault, string staffIds)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime actualDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(selectedDate, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out actualDate))
                {
                    actualDate = DateTime.Now;
                }

                int[] ids = new int[] { 0 };
                if (staffIds != null && staffIds != "")
                    ids = staffIds.Split(',').Select(x => Convert.ToInt32(x)).ToArray();

                RotaAdminDto rotaAdminDto = new RotaAdminDto();
                rotaAdminDto.StaffsForRotaAdmin = rotaService.GetStaffsForRotaAdmin(searchKey, actualDate, ids);
                if (IsDefault)
                {
                    return PartialView("StaffSearch", rotaAdminDto);
                }
                else
                {
                    return PartialView("MatchingStaffResult", rotaAdminDto);
                }
            }
        }
        #endregion

        #region GetRoomsForRotaAdmin
        [HttpPost]
        public ActionResult GetRoomsForRotaAdmin(string searchKey, string selectedDate, bool IsDefault, string roomIds)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime actualDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(selectedDate, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out actualDate))
                {
                    actualDate = DateTime.Now;
                }

                int[] ids = new int[] { 0 };
                if (roomIds != null && roomIds != "")
                    ids = roomIds.Split(',').Select(x => Convert.ToInt32(x)).ToArray();

                RotaAdminDto rotaAdminDto = new RotaAdminDto();
                rotaAdminDto.RoomsForRotaAdmin = rotaService.GetRoomsForRotaAdmin(searchKey, actualDate, ids);
                if (IsDefault)
                {
                    return PartialView("RoomSearch", rotaAdminDto);
                }
                else
                {
                    return PartialView("MatchingRoomResult", rotaAdminDto);
                }
            }
        }
        #endregion

        #region UpdateStaffRotaFromDiaryView
        [HttpPost]
        public ActionResult UpdateStaffRotaFromDiaryView(UpdateStaffRotaDto updateStaffRota)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                DateTime actualDate = DateTime.Now;
                if (!DateTime.TryParseExact(updateStaffRota.RotaDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out actualDate))
                {
                    updateStaffRota.RotaDate = DateTime.Now;
                }
                else
                {
                    updateStaffRota.RotaDate = actualDate;
                }
                if (updateStaffRota.ChangedStartTime != null && updateStaffRota.ChangedEndTime != null)
                {
                    updateStaffRota.ChangedStartDateTime = updateStaffRota.RotaDate + updateStaffRota.ChangedStartTime;
                    updateStaffRota.ChangedEndDateTime = updateStaffRota.RotaDate + updateStaffRota.ChangedEndTime.Add(new TimeSpan(0, 15, 0));
                }
                DateTime newDate = new DateTime(updateStaffRota.RotaDate.Year, updateStaffRota.RotaDate.Month, updateStaffRota.RotaDate.Day, 23, 45, 0);

                if (updateStaffRota.StaffRotas != null)
                {
                    updateStaffRota.StaffRotas.Where(x => x.EndTime == null).ToList().ForEach(x => x.EndTime = newDate);
                    foreach (var rota in updateStaffRota.StaffRotas.Where(x => x.EndTime != null && x.StartTime != null).ToList())
                    {
                        if (!DateTime.TryParseExact(rota.DateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out actualDate))
                        {
                            rota.Date = DateTime.Now;
                        }
                        else
                        {
                            rota.Date = actualDate;
                        }
                        rota.Duration = rota.EndTime.Value.TimeOfDay.Subtract(rota.StartTime.Value.TimeOfDay);
                        rota.StartTime = rota.Date + rota.StartTime.Value.TimeOfDay;
                        rota.EndTime = rota.Date + rota.EndTime.Value.TimeOfDay;
                    }
                }
                int loggedInUserId = this.GetLoggedInUserId();
                UpdateStaffRotaResponseDto updateStaffRotaResponse = rotaService.UpdateStaffRotaFromDiaryView(updateStaffRota, loggedInUserId);
                return Json(new { updateStaffRotaResponse });
            }
        }
        #endregion

        #region UpdateRoomRotaFromDiaryView
        [HttpPost]
        public ActionResult UpdateRoomRotaFromDiaryView(UpdateRoomRotaDto updateRoomRota)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                DateTime actualDate = DateTime.Now;
                if (!DateTime.TryParseExact(updateRoomRota.RotaDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out actualDate))
                {
                    updateRoomRota.RotaDate = DateTime.Now;
                }
                else
                {
                    updateRoomRota.RotaDate = actualDate;
                }
                if (updateRoomRota.ChangedStartTime != null && updateRoomRota.ChangedEndTime != null)
                {
                    updateRoomRota.ChangedStartDateTime = updateRoomRota.RotaDate + updateRoomRota.ChangedStartTime;
                    updateRoomRota.ChangedEndDateTime = updateRoomRota.RotaDate + updateRoomRota.ChangedEndTime.Add(new TimeSpan(0, 15, 0));
                }
                DateTime newDate = new DateTime(updateRoomRota.RotaDate.Year, updateRoomRota.RotaDate.Month, updateRoomRota.RotaDate.Day, 23, 45, 0);

                if (updateRoomRota.RoomRotas != null)
                {
                    updateRoomRota.RoomRotas.Where(x => x.EndTime == null).ToList().ForEach(x => x.EndTime = newDate);
                    foreach (var rota in updateRoomRota.RoomRotas.Where(x => x.EndTime != null && x.StartTime != null).ToList())
                    {
                        if (!DateTime.TryParseExact(rota.DateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out actualDate))
                        {
                            rota.Date = DateTime.Now;
                        }
                        else
                        {
                            rota.Date = actualDate;
                        }
                        rota.Duration = rota.EndTime.Value.TimeOfDay.Subtract(rota.StartTime.Value.TimeOfDay);
                        rota.StartTime = rota.Date + rota.StartTime.Value.TimeOfDay;
                        rota.EndTime = rota.Date + rota.EndTime.Value.TimeOfDay;
                    }
                }
                int loggedInUserId = this.GetLoggedInUserId();
                UpdateRoomRotaResponseDto updateRoomRotaResponse = rotaService.UpdateRoomRotaFromDiaryView(updateRoomRota, loggedInUserId);
                return Json(new { updateRoomRotaResponse });
            }
        }
        #endregion

        #region WhetherConflictsExistForActivity
        [HttpPost]
        public ActionResult WhetherConflictsExistForActivity(ActivityExceptionParametersDto parameters)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                ActivityExceptionDetailsDto exceptionDetails = new ActivityExceptionDetailsDto();

                if (parameters.StaffRotas != null)
                    parameters.StaffRotas = parameters.StaffRotas.Where(x => !x.IsWorking).ToList();
                if (parameters.RoomRotas != null)
                    parameters.RoomRotas = parameters.RoomRotas.Where(x => !x.IsWorking).ToList();
                if (parameters.SelectedDateString != null)
                {
                    string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                    DateTime actualDate = DateTime.Now;
                    if (!DateTime.TryParseExact(parameters.SelectedDateString, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out actualDate))
                    {
                        parameters.SelectedDate = DateTime.Now;
                    }
                    else
                    {
                        parameters.SelectedDate = actualDate;
                    }

                }

                exceptionDetails = rotaService.WhetherConflictsExistForActivity(parameters);
                return Json(new { exceptionDetails });
            }
        }
        #endregion

        #region Get RSI Point Value And Status
        [HttpGet]
        public ActionResult RSIPointsAndStatus(string startDateTime, int staffID)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                DateTime activityDate = DateTime.Now;
                string[] dateFormatArray = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                if (!DateTime.TryParseExact(startDateTime, dateFormatArray, CultureInfo.InvariantCulture, DateTimeStyles.None, out activityDate))
                {
                    activityDate = DateTime.Now;
                }
                RespontMessageDto responseMessage = bookingService.GetRSIPointValueAndStatus(activityDate, staffID);
                if (responseMessage != null)
                {
                    return Json(new
                    {
                        ResponseID = responseMessage.ResponseID,
                        ResponseMessage = responseMessage.ResponseMessage
                    }, JsonRequestBehavior.AllowGet);
                }
                return new HttpNotFoundResult();
            }
        }
        #endregion

        #region CancelInternalBooking
        public ActionResult CancelInternalBooking(int bookingId)
        {
            int response = bookingService.CancelInternalBooking(bookingId);
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region CancelClass
        public ActionResult CancelClass(int rotaId)
        {
            int response = bookingService.CancelClass(rotaId);
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Update Staff's Rsi Point
        [HttpPost]
        public ActionResult UpdateRsiPoint(int? staffId, decimal? rsiPoint)
        {
            using (var logger = new MethodEntryExitLogger())
            {
                int response = bookingService.RsiPointUpdation(staffId, rsiPoint);
                if (response > 0)
                {
                    return Json(new { ResponseValue = response, ResponseMessage = Common.Resources.Messages.RSIPOINT_UPDATION_SUCCESS_MSG });
                }
                return Json(new { ResponseValue = response, ResponseMessage = Common.Resources.Messages.RSIPOINT_UPDATION_FAILURE_MSG });
            }
        }
        #endregion
    }
}
