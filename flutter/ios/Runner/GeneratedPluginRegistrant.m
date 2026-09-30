//
//  Generated file. Do not edit.
//

// clang-format off

#import "GeneratedPluginRegistrant.h"

#if __has_include(<flutter_foreground_task/FlutterForegroundTaskPlugin.h>)
#import <flutter_foreground_task/FlutterForegroundTaskPlugin.h>
#else
@import flutter_foreground_task;
#endif

#if __has_include(<flutter_secure_storage/FlutterSecureStoragePlugin.h>)
#import <flutter_secure_storage/FlutterSecureStoragePlugin.h>
#else
@import flutter_secure_storage;
#endif

#if __has_include(<network_info_plus/FPPNetworkInfoPlusPlugin.h>)
#import <network_info_plus/FPPNetworkInfoPlusPlugin.h>
#else
@import network_info_plus;
#endif

#if __has_include(<permission_handler_apple/PermissionHandlerPlugin.h>)
#import <permission_handler_apple/PermissionHandlerPlugin.h>
#else
@import permission_handler_apple;
#endif

#if __has_include(<printing/PrintingPlugin.h>)
#import <printing/PrintingPlugin.h>
#else
@import printing;
#endif

#if __has_include(<shared_preferences_foundation/SharedPreferencesPlugin.h>)
#import <shared_preferences_foundation/SharedPreferencesPlugin.h>
#else
@import shared_preferences_foundation;
#endif

#if __has_include(<thermal_printer_plus/ThermalPrinterPlugin.h>)
#import <thermal_printer_plus/ThermalPrinterPlugin.h>
#else
@import thermal_printer_plus;
#endif

@implementation GeneratedPluginRegistrant

+ (void)registerWithRegistry:(NSObject<FlutterPluginRegistry>*)registry {
  [FlutterForegroundTaskPlugin registerWithRegistrar:[registry registrarForPlugin:@"FlutterForegroundTaskPlugin"]];
  [FlutterSecureStoragePlugin registerWithRegistrar:[registry registrarForPlugin:@"FlutterSecureStoragePlugin"]];
  [FPPNetworkInfoPlusPlugin registerWithRegistrar:[registry registrarForPlugin:@"FPPNetworkInfoPlusPlugin"]];
  [PermissionHandlerPlugin registerWithRegistrar:[registry registrarForPlugin:@"PermissionHandlerPlugin"]];
  [PrintingPlugin registerWithRegistrar:[registry registrarForPlugin:@"PrintingPlugin"]];
  [SharedPreferencesPlugin registerWithRegistrar:[registry registrarForPlugin:@"SharedPreferencesPlugin"]];
  [ThermalPrinterPlugin registerWithRegistrar:[registry registrarForPlugin:@"ThermalPrinterPlugin"]];
}

@end
