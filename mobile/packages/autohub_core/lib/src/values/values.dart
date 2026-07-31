/// Value semantics ported from the C# SharedKernel.
///
/// These are the types where the phone and the server must agree exactly, because disagreement is
/// visible to a user as a wrong number or a rejected plate rather than as an error.
library;

export 'money.dart';
export 'parse_result.dart';
export 'registration_number.dart';
