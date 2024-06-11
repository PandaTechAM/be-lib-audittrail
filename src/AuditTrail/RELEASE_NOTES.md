# Release Notes

## Version 1.2.7 - 2024-06-11
- Removed methpd `BeforeTransactionCommitedAsync`.     
- Added method `ConsumeAsync` with transaction parameters.		

## Version 1.2.0 - 2024-05-29
- Added method `BeforeTransactionCommitedAsync` added for transaction support in the audit trail consumer service.
- Removed method `ConsumeTransactionAsync` added for transaction support in the audit trail consumer service.

Action Required: 
- Implementation: All classes dealing with audit trail data AuditTrailConsumer must implement the BeforeTransactionCommitedAsync method.

## Version 1.1.6 - 2024-05-28
- Added method `ConsumeTransactionAsync` added for transaction support in the audit trail consumer service.

Action Required: 
- Implementation: All classes dealing with audit trail data AuditTrailConsumer must implement the ConsumeTransactionAsync method.

## Version 1.1.4 - 2024-05-27

### Added
- Added method BeforeSaveAsync parameter `DbContextEventData` added for transaction support in the audit trail service.

### Changed
- Refactored naming conventions for consistency.
- Optimized LINQ queries for better performance.

### Fixed
- Resolved issue with incorrect entity state handling.
- Fixed bug in `GetEntityId` method for better null handling.

### Renamed
- Namespace `AuditTrail.Fluent.Abstraction` renamed to `AuditTrail.Fluent.Abstractions`.