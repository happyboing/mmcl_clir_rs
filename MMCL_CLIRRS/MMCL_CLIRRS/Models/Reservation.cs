using System;
using System.Collections.Generic;
using System.Text;

namespace MMCL_CLIRRS
{
    public class Reservation
    {
        public int ReservationId { get; }
        public string UserName { get; }
        public string UserIdNumber { get; }       // student/faculty ID
        public UserType UserType { get; }
        public string Department { get; }
        public string Program { get; }
        public int YearLevel { get; }
        public string SpotOrRoom { get; }         // e.g. "Table 3 - Seat 2" or "Discussion Room 1"
        public bool IsDiscussionRoom { get; }
        public DateTime Date { get; }
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public ReservationStatus Status { get; private set; }

        public Reservation(int id, string userName, string userIdNumber, UserType userType,
                           string department, string program, int yearLevel,
                           string spotOrRoom, bool isDiscussionRoom,
                           DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            if (endTime <= startTime)
                throw new ArgumentException("End time must be after the start time.");

            ReservationId = id;
            UserName = userName;
            UserIdNumber = userIdNumber;
            UserType = userType;
            Department = department;
            Program = program;
            YearLevel = yearLevel;
            SpotOrRoom = spotOrRoom;
            IsDiscussionRoom = isDiscussionRoom;
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            Status = ReservationStatus.Active;
        }

        public void Cancel() => Status = ReservationStatus.Cancelled;
        public void MarkExpired() => Status = ReservationStatus.Expired;
        public void MarkCompleted() => Status = ReservationStatus.Completed;
    }
}
