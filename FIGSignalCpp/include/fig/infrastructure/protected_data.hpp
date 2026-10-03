#pragma once
#include <string>
#include <string_view>

namespace fig
{
// Compatible with FIGCommon.Utilities.ProtectedDataUtil.Unprotect:
// Base64(nonce[12] + UTF-8 ciphertext + tag[16]), AES-256-GCM, FIG_MASTER_KEY.
[[nodiscard]] std::string unprotect(std::string_view encrypted);
void protected_data_self_test();
} // namespace fig
