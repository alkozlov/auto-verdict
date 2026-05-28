# Payments Feature

As part of this feature, we are implementing the ability to purchase report generation / car listing checks for money.

If a user is on the `whitelist`, they can perform car checks without restrictions or payments. For all other users, payment is required.

To order a car check, the user must have credits on their account. The cost of 1 check is 1 credit. Users can purchase credits through the payment system integrated into the application. Currently, the application is integrated only with the `Lemon Squeezy` payment system.

Users can purchase credits through the application interface by selecting the desired number of credits and paying via `Lemon Squeezy`. After a successful payment, the credits will be added to the user's account, and they can use them to order car checks.

The application offers 2 credit packages for purchase:
1. 1 credit — 1 check — 20 PLN
2. 3 credits — 3 checks — 40 PLN

**IMPORTANT**: Credits are non-refundable and cannot be exchanged for money. Credits are only deducted when the user's request has been successfully processed and the check result has been provided. If the request cannot be processed (e.g., due to a technical error), credits will not be deducted, and the user can retry without additional cost.

An authenticated user can see their balance at the bottom of the page (already implemented). Next to this area, there should be a "Top up balance" button that opens the interface for purchasing credits. After a successful payment, the user's balance is updated and they can use the credits to order car checks.

Previously, I have already done integration with the `Lemon Squeezy` service as part of another project. Below are links to the repository and files with code that can be used as a reference (they work and have been tested):
- Repository: `C:\Users\AKazlou\Projects\project-revenge\`
- Documentation for `Lemon Squeezy` integration: `C:\Users\AKazlou\Projects\project-revenge\docs\lemon-squeezy-integration.md`

Based on this information, you need to develop a plan and implement the `Lemon Squeezy` integration for purchasing credits in our application. Make sure the credit purchase process is simple and convenient for users, and that all transactions are secure and reliable.
