#!/usr/bin/env bash
# Clean Architecture solution scaffold (.NET 8)
# Kullanım:
#   ./scaffold-solution.sh
#   ./scaffold-solution.sh BilisimPersoneli
#   ./scaffold-solution.sh BilisimPersoneli ./bilisimPersoneli
#   ./scaffold-solution.sh BilisimPersoneli .

set -euo pipefail

TFM="net8.0"

# ---------------------------------------------------------------------------
# Proje adı (zorunlu)
# ---------------------------------------------------------------------------
if [[ $# -ge 1 && -n "${1:-}" ]]; then
  PROJECT_NAME="$1"
else
  read -r -p "Proje adı (ör. BilisimPersoneli): " PROJECT_NAME
fi

PROJECT_NAME="$(echo "${PROJECT_NAME}" | xargs)"

if [[ -z "${PROJECT_NAME}" ]]; then
  echo "Hata: Proje adı boş olamaz."
  exit 1
fi

# Geçerli C# / .NET proje adı: harf veya _ ile başlar; harf, rakam, _
if [[ ! "${PROJECT_NAME}" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
  echo "Hata: Geçersiz proje adı '${PROJECT_NAME}'."
  echo "      Harf veya _ ile başlamalı; yalnızca harf, rakam ve _ içerebilir."
  exit 1
fi

# ---------------------------------------------------------------------------
# Hedef dizin (opsiyonel; varsayılan: ./ProjeAdi)
# ---------------------------------------------------------------------------
if [[ $# -ge 2 && -n "${2:-}" ]]; then
  ROOT_DIR="$2"
else
  DEFAULT_ROOT="./${PROJECT_NAME}"
  if [[ $# -ge 1 ]]; then
    ROOT_DIR="${DEFAULT_ROOT}"
  else
    read -r -p "Hedef dizin [${DEFAULT_ROOT}]: " ROOT_DIR_INPUT
    ROOT_DIR="${ROOT_DIR_INPUT:-${DEFAULT_ROOT}}"
  fi
fi

DOMAIN_PROJ="${PROJECT_NAME}.Domain"
APP_PROJ="${PROJECT_NAME}.Application"
INFRA_PROJ="${PROJECT_NAME}.Infrastructure"
WEB_PROJ="${PROJECT_NAME}.Web"

DOMAIN_DIR="src/${DOMAIN_PROJ}"
APP_DIR="src/${APP_PROJ}"
INFRA_DIR="src/${INFRA_PROJ}"
WEB_DIR="src/${WEB_PROJ}"

echo ""
echo "==> Proje adı : ${PROJECT_NAME}"
echo "==> Hedef     : ${ROOT_DIR}"
echo ""

mkdir -p "${ROOT_DIR}"
cd "${ROOT_DIR}"

if [[ -f "${PROJECT_NAME}.sln" || -f "${PROJECT_NAME}.slnx" ]]; then
  echo "Hata: ${PROJECT_NAME}.sln(x) zaten var. İptal edildi."
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Hata: 'dotnet' bulunamadı. .NET 8 SDK kurulu olmalı."
  exit 1
fi

echo "==> SDK: $(dotnet --version)"

# ---------------------------------------------------------------------------
# 1.3 Solution ve projeler
# ---------------------------------------------------------------------------
echo "==> Solution ve projeler oluşturuluyor..."

# SDK 9+ varsayılanı .slnx; klasik .sln için --format sln kullan
if dotnet new sln --help 2>&1 | grep -q -- '--format'; then
  dotnet new sln -n "${PROJECT_NAME}" --format sln
else
  dotnet new sln -n "${PROJECT_NAME}"
fi

# Oluşan solution dosyasını bul (.sln veya .slnx)
if [[ -f "${PROJECT_NAME}.sln" ]]; then
  SLN_FILE="${PROJECT_NAME}.sln"
elif [[ -f "${PROJECT_NAME}.slnx" ]]; then
  SLN_FILE="${PROJECT_NAME}.slnx"
else
  echo "Hata: Solution dosyası oluşmadı (${PROJECT_NAME}.sln / .slnx)."
  exit 1
fi
echo "==> Solution  : ${SLN_FILE}"

dotnet new classlib -n "${DOMAIN_PROJ}" -o "${DOMAIN_DIR}" -f "${TFM}"
dotnet new classlib -n "${APP_PROJ}" -o "${APP_DIR}" -f "${TFM}"
dotnet new classlib -n "${INFRA_PROJ}" -o "${INFRA_DIR}" -f "${TFM}"
dotnet new mvc -n "${WEB_PROJ}" -o "${WEB_DIR}" -f "${TFM}"

dotnet sln "${SLN_FILE}" add "${DOMAIN_DIR}/${DOMAIN_PROJ}.csproj"
dotnet sln "${SLN_FILE}" add "${APP_DIR}/${APP_PROJ}.csproj"
dotnet sln "${SLN_FILE}" add "${INFRA_DIR}/${INFRA_PROJ}.csproj"
dotnet sln "${SLN_FILE}" add "${WEB_DIR}/${WEB_PROJ}.csproj"

rm -f "${DOMAIN_DIR}/Class1.cs"
rm -f "${APP_DIR}/Class1.cs"
rm -f "${INFRA_DIR}/Class1.cs"

# ---------------------------------------------------------------------------
# 1.4 Proje referansları
# ---------------------------------------------------------------------------
echo "==> Proje referansları ekleniyor..."

dotnet add "${APP_DIR}/${APP_PROJ}.csproj" \
  reference "${DOMAIN_DIR}/${DOMAIN_PROJ}.csproj"

dotnet add "${INFRA_DIR}/${INFRA_PROJ}.csproj" \
  reference "${APP_DIR}/${APP_PROJ}.csproj"

dotnet add "${INFRA_DIR}/${INFRA_PROJ}.csproj" \
  reference "${DOMAIN_DIR}/${DOMAIN_PROJ}.csproj"

dotnet add "${WEB_DIR}/${WEB_PROJ}.csproj" \
  reference "${APP_DIR}/${APP_PROJ}.csproj"

dotnet add "${WEB_DIR}/${WEB_PROJ}.csproj" \
  reference "${INFRA_DIR}/${INFRA_PROJ}.csproj"

# ---------------------------------------------------------------------------
# 1.5 NuGet paketleri
# ---------------------------------------------------------------------------
echo "==> NuGet paketleri ekleniyor..."

dotnet add "${APP_DIR}/${APP_PROJ}.csproj" package AutoMapper --version 15.1.4
dotnet add "${APP_DIR}/${APP_PROJ}.csproj" package System.ComponentModel.Annotations --version 5.0.0

dotnet add "${INFRA_DIR}/${INFRA_PROJ}.csproj" package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add "${INFRA_DIR}/${INFRA_PROJ}.csproj" package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add "${INFRA_DIR}/${INFRA_PROJ}.csproj" package Microsoft.EntityFrameworkCore.Design --version 8.0.11

dotnet add "${WEB_DIR}/${WEB_PROJ}.csproj" package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add "${WEB_DIR}/${WEB_PROJ}.csproj" package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add "${WEB_DIR}/${WEB_PROJ}.csproj" package Microsoft.EntityFrameworkCore.Design --version 8.0.11

INFRA_CSPROJ="${INFRA_DIR}/${INFRA_PROJ}.csproj"
if ! grep -q 'Microsoft.AspNetCore.App' "${INFRA_CSPROJ}"; then
  echo "==> FrameworkReference (Microsoft.AspNetCore.App) ekleniyor..."
  if grep -q '</Project>' "${INFRA_CSPROJ}"; then
    awk '
      /<\/Project>/ && !done {
        print "  <ItemGroup>"
        print "    <FrameworkReference Include=\"Microsoft.AspNetCore.App\" />"
        print "  </ItemGroup>"
        print ""
        done=1
      }
      { print }
    ' "${INFRA_CSPROJ}" > "${INFRA_CSPROJ}.tmp"
    mv "${INFRA_CSPROJ}.tmp" "${INFRA_CSPROJ}"
  else
    echo "Uyarı: ${INFRA_CSPROJ} içinde </Project> bulunamadı; FrameworkReference eklenemedi."
  fi
else
  echo "==> FrameworkReference zaten mevcut, atlanıyor."
fi

# ---------------------------------------------------------------------------
# 1.6 Alt klasörler
# ---------------------------------------------------------------------------
echo "==> Alt klasörler oluşturuluyor..."

mkdir -p "${DOMAIN_DIR}/Common" "${DOMAIN_DIR}/Entities" "${DOMAIN_DIR}/Enums"
mkdir -p "${APP_DIR}/Common" "${APP_DIR}/DTOs" "${APP_DIR}/Interfaces" "${APP_DIR}/Mapping"
mkdir -p "${INFRA_DIR}/Persistence/Migrations" "${INFRA_DIR}/Services"
mkdir -p "${WEB_DIR}/Controllers/Api" "${WEB_DIR}/Services"

# ---------------------------------------------------------------------------
# Doğrulama
# ---------------------------------------------------------------------------
echo "==> Solution derleniyor (doğrulama)..."
dotnet build "${SLN_FILE}" --nologo -v q

echo ""
echo "Tamamlandı."
echo "Dizin     : $(pwd)"
echo "Solution  : ${SLN_FILE}"
echo "Projeler  :"
echo "  - ${DOMAIN_PROJ}"
echo "  - ${APP_PROJ}"
echo "  - ${INFRA_PROJ}"
echo "  - ${WEB_PROJ}"
echo ""
echo "Sonraki adım:"
echo "  cd $(pwd)"
echo "  dotnet run --project ${WEB_DIR}"
