/// The outcome of turning user input into a value object.
///
/// A sealed result rather than a nullable return, because *why* the input failed is load-bearing.
/// `registration.required` asks the user to type something; `registration.malformed` tells them
/// that what they typed is not a plate. Those are two different sentences, resolved from two
/// different i18n keys, and a `null` return collapses them into one — which in practice means the
/// sell flow shows "check the digits" to a user who has not typed any.
///
/// The codes are the server's codes. A client that invents its own vocabulary for the same failure
/// forces support to learn two.
sealed class ParseResult<T> {
  const ParseResult();

  /// The parsed value, or `null` when parsing failed.
  T? get valueOrNull => switch (this) {
        ParseOk<T>(:final value) => value,
        ParseInvalid<T>() => null,
      };

  /// The stable machine code of the failure, or `null` on success. Branch on this, never on text.
  String? get errorCode => switch (this) {
        ParseOk<T>() => null,
        ParseInvalid<T>(:final code) => code,
      };

  bool get isValid => this is ParseOk<T>;
}

final class ParseOk<T> extends ParseResult<T> {
  const ParseOk(this.value);

  final T value;
}

final class ParseInvalid<T> extends ParseResult<T> {
  const ParseInvalid(this.code, this.message);

  /// `registration.malformed`, `money.not_representable` — the same code the server raises for the
  /// same input, so one machine vocabulary spans client and server.
  final String code;

  /// Pre-resolved human text. Displayed, never branched on; the registry rewords it freely.
  final String message;
}
