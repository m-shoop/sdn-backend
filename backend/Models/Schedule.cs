namespace SdnBackend.Models;

public class Schedule(Technician techAccount, Salon salon, int numDaysPriorReleased, DateOnly effStartDate, List<DayTimeRange> dayTimeRanges, int frequency)
{
    public int Id { get; set; }
    public Technician TechAccount { get; init; } = techAccount;
    public Salon Salon { get; init; } = salon;
    public int NumDaysPriorReleased { get; set; } = numDaysPriorReleased;
    public DateOnly EffStartDate { get; set; } = effStartDate;
    public DateOnly EffEndDate { get; set; }
    public bool Outage { get; set; }
    public List<DayTimeRange> DayTimeRanges { get; set; } = dayTimeRanges;
    public int Frequency { get; set; } = frequency;

    // method to retrieve availability for a specific date
    public List<DayTimeRange> GetAvailByDate(DateOnly date)
    {
        List<DayTimeRange> dateTimeRangeList = new List<DayTimeRange>();
        // if the date is outside the schedule's range, return empty
        // need to consider outages at some point
        if ( date < EffStartDate || (EffEndDate > default(DateOnly) && date > EffEndDate) )
        {
            return dateTimeRangeList;
        }

        // otherwise, we're in the schedule's range! return the available hours
        else 
        {
            var dayOfWeek = date.DayOfWeek; 
            var cnt = 0;
            foreach (DayTimeRange dayTimeRange in dateTimeRangeList)
            {
                if (dayTimeRange.Day == dayOfWeek)
                {
                    dateTimeRangeList[cnt] = dayTimeRange;
                    cnt++;
                }
            }

            return dateTimeRangeList;
        }
    }

    public List<TimeOnly> GetAvailableStartTimesOnDate(DateOnly date, int duration, List<Agreement> techAgreementsOnDate)
    {
        DateTime now = DateTime.Now;
        List<TimeOnly> availableStartTimes = new List<TimeOnly>();
        // first check if the date is valid for this schedule; if not return empty list
        if ( date < EffStartDate || (EffEndDate > default(DateOnly) && date > EffEndDate) )
        {
            return availableStartTimes;
        }
        // otherwise we possible have some options, start at first time range in DayTimeRange and in 5-minute increments
        // add the starting time that would be possible for this duration
        else
        {
            // loop through each DayTimeRange in list variable
            foreach (DayTimeRange dayTimeRange in DayTimeRanges)
            {
                if (date.DayOfWeek == dayTimeRange.Day){
                    TimeOnly currTime;
                    TimeOnly currentTimeOfDay = TimeOnly.FromDateTime(now);

                    // initialize currTime to beginning time of this DayTimeRange for future dates
                    if (date > DateOnly.FromDateTime(now))
                    {
                        currTime = dayTimeRange.BeginTime;
                    }
                    // otherwise this is today's date and we should only look at appointments
                    // beginning after the current time
                    else
                    {
                        // Skip this time range if it has already begun
                        if (currentTimeOfDay > dayTimeRange.BeginTime)
                        {
                            continue;
                        }

                        // Otherwise, use the BeginTime of today's dayTimeRange
                        else 
                        {
                            currTime = dayTimeRange.BeginTime;
                        }
                    }
                    // make sure we haven't reached the end time
                    while (currTime.AddMinutes(duration) <= dayTimeRange.EndTime)
                    {
                        availableStartTimes.Add(currTime);
                        currTime = currTime.AddMinutes(5);
                    }
                }
            }
        }
        // if the tech has no appointments on this date, return all available times
        if (techAgreementsOnDate.Count == 0)
        {
            return availableStartTimes;
        }
        // if the tech does have appointments, make sure the available times returned do not overlap
        return RemoveOverlappingAppointments(availableStartTimes, duration, techAgreementsOnDate);
    }

    private static List<TimeOnly> RemoveOverlappingAppointments(List<TimeOnly> availableStartTimes, int availDuration, List<Agreement> techAgreementsOnDate)
    {
        List<TimeOnly> updatedAvailableStartTimes = new List<TimeOnly>();
        // need to cycle through every TimeOnly in availableStartTimes, and compare its start time and end time against the existing agreements on that date
        foreach (TimeOnly availableStart in availableStartTimes)
        {
            bool conflict = false;

            foreach (Agreement agreement in techAgreementsOnDate)
            {
                TimeOnly agreementStart = agreement.Time;
                TimeOnly agreementEnd = agreement.Time.AddMinutes(agreement.Service.Duration);
                TimeOnly availableEnd = availableStart.AddMinutes(availDuration);

                if (agreementStart >= availableStart && agreementStart < availableEnd)
                {
                    // do not add this to the list
                    conflict = true;
                    break;
                }

                if (agreementEnd <= availableEnd && agreementEnd > availableStart)
                {
                    // do not add this to the list
                    conflict = true;
                    break;
                }

                if (availableStart >= agreementStart && availableStart < agreementEnd)
                {
                    // do not add this to the list
                    conflict = true;
                    break;
                }

                if (availableEnd <= agreementEnd && availableEnd > agreementStart)
                {
                    // do not add this to the list
                    conflict = true;
                    break; 
                }
            }

            // only if there were no conflicts with any of the agreements do we let the time slot through
            if (!conflict)
            {
                updatedAvailableStartTimes.Add(availableStart);
            }
        }
        
        return updatedAvailableStartTimes;
    }

    public override string ToString() => $"Tech: {TechAccount}; Salon: {Salon} (EffStartDate: {EffStartDate})";

}