// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'problem.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$ProblemFieldErrorImpl _$$ProblemFieldErrorImplFromJson(
  Map<String, dynamic> json,
) => _$ProblemFieldErrorImpl(
  code: json['code'] as String,
  pointer: json['pointer'] as String?,
  message: json['message'] as String?,
  messageKey: json['messageKey'] as String?,
  params: json['params'] as Map<String, dynamic>?,
);

Map<String, dynamic> _$$ProblemFieldErrorImplToJson(
  _$ProblemFieldErrorImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'pointer': instance.pointer,
  'message': instance.message,
  'messageKey': instance.messageKey,
  'params': instance.params,
};

_$ProblemDetailsImpl _$$ProblemDetailsImplFromJson(Map<String, dynamic> json) =>
    _$ProblemDetailsImpl(
      type: json['type'] as String?,
      title: json['title'] as String?,
      status: (json['status'] as num?)?.toInt(),
      code: json['code'] as String?,
      detail: json['detail'] as String?,
      instance: json['instance'] as String?,
      traceId: json['traceId'] as String?,
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] as String?,
      errors: (json['errors'] as List<dynamic>?)
          ?.map((e) => ProblemFieldError.fromJson(e as Map<String, dynamic>))
          .toList(),
      metadata: json['metadata'] as Map<String, dynamic>?,
      retryable: json['retryable'] as bool?,
      retryAfterSeconds: (json['retryAfterSeconds'] as num?)?.toInt(),
      helpUrl: json['helpUrl'] as String?,
      supportReference: json['supportReference'] as String?,
    );

Map<String, dynamic> _$$ProblemDetailsImplToJson(
  _$ProblemDetailsImpl instance,
) => <String, dynamic>{
  'type': instance.type,
  'title': instance.title,
  'status': instance.status,
  'code': instance.code,
  'detail': instance.detail,
  'instance': instance.instance,
  'traceId': instance.traceId,
  'requestId': instance.requestId,
  'timestamp': instance.timestamp,
  'errors': instance.errors,
  'metadata': instance.metadata,
  'retryable': instance.retryable,
  'retryAfterSeconds': instance.retryAfterSeconds,
  'helpUrl': instance.helpUrl,
  'supportReference': instance.supportReference,
};
