using SdnBackend.Models;
using SdnBackend.Repositories;
using Npgsql;

namespace SdnBackend.Services;

public class ConfirmFilingService(NpgsqlDataSource dataSource, string confirmTokenHash)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly string _confirmTokenHash = confirmTokenHash;

    // ProcessConfirmationToken is a method to handle a confirmation token passed in from the frontend
    // Typically when a user clicks a confirmation link in their confirmation email.
    // Return values:
    //  0 - Success
    //  1 - No appointment found with this token
    //  2 - Token expired, new email sent
    //  3 - Appointment canceled
    //  4 - Failed (appointment in invalid state)
    public async Task<int> ProcessConfirmationToken(IEmailService emailService)
    {
        // first spin up Agreement repository
        AgreementRepository agreeRepo = new AgreementRepository(_dataSource);

        // look up agreement by token hash
        Agreement? agreement = await agreeRepo.GetByConfirmationTokenHash(_confirmTokenHash);

        // if no agreement found, return error
        if (agreement == null)
        {
            return 1; // not found
        }

        int appointmentStatus = (int)agreement.ApptStatus;

        // the agreement is pending, so mark it as confirmed and store to the database
        if (appointmentStatus == 0)
        {
            // mark as confirmed
            agreement.Confirm();
            // update the entity in the database
            await agreeRepo.UpdateEntity(agreement);
            // send a final confirmation email
            ConfirmationEmail email = new ConfirmationEmail
            {
                To = agreement.Client.Email,
                AppointmentTime = new DateTime(agreement.Date, agreement.Time)
            };
            await emailService.SendConfirmationAsync(email, 2);

            return 0; // success
        }

        // the agreement is confirmed, so return success
        if (appointmentStatus == 1)
        {
            // send a final confirmation email
            ConfirmationEmail email = new ConfirmationEmail
            {
                To = agreement.Client.Email,
                AppointmentTime = new DateTime(agreement.Date, agreement.Time)
            };
            await emailService.SendConfirmationAsync(email, 2);
            return 0; // success
        }

        // the agreement is expired, so send a new email and return an error
        if (appointmentStatus == 2)
        {
            ConfirmationEmail confEmail = new ConfirmationEmail
            {
                To = agreement.Client.Email,
                ConfirmationToken = agreement.MarkPending(),
                AppointmentTime = DateTime.UtcNow

            };

            await emailService.SendConfirmationAsync(confEmail, 1);
            return 2; // expired
        }

        // the agreement is cancelled, so return an error
        if (appointmentStatus == 3)
        {
            return 3; // cancelled
        }

        return 4; // failed (bad state)

    }
}

