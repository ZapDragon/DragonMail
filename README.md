# DragonMail

I am writing this Mail server with Two major focuses.
Security And Redundancy.

# Security
E-Mail is a very old technology, and too many people take it for granted,
esecpially since they never consider all the verification, bank statement, password reset,
and other sensitive data that is handled by it.

There is no Reason Anyone should have to fear for the security of their personal data.

This mail server will implement an extremely easy method for finally combating spam and malicious content you can and will
eventually recive.

# Redundancy
E-Mail Server Solutions that implement True reliable redundancy either cost a fortune,
are very computationally expensive to deploy and maintain, or both.

This Mail Server will not only be very reasource light, but work accross platforms, using Mono on Non-Microsoft environments.

Multiple deployed servers will communicate directly between one another and deligate Smtp Client and Imap transactions,
and share Mailbox changes, global configuration changes, detailed logs of every recipient and sender of any given email.

Servers that even go offline for an amount of time are later updated when they are brought back online.



