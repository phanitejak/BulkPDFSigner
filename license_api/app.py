from flask import Flask, request, jsonify
import gspread
import google.auth
from google.oauth2 import service_account
from datetime import datetime, timedelta
import os
import json

app = Flask(__name__)

SCOPES = [
    'https://spreadsheets.google.com/feeds',
    'https://www.googleapis.com/auth/drive',
]


def _get_credentials():
    """Resolve Sheets credentials.

    SERVICE_JSON env var (Render today) takes precedence; otherwise fall back
    to Application Default Credentials (Cloud Run, GCE, or
    `gcloud auth application-default login` locally). Either path works
    without code changes.
    """
    raw = os.environ.get("SERVICE_JSON")
    if raw:
        info = json.loads(raw)
        return service_account.Credentials.from_service_account_info(info, scopes=SCOPES)
    creds, _ = google.auth.default(scopes=SCOPES)
    return creds


client = gspread.authorize(_get_credentials())

LICENSE_SHEET_ID = "1FKnY8mhgBd8cbHmAORP0BjeiwxSLnMF1zPEnCW2H_a4"
LICENSE_SHEET = "Licenses"
LOG_SHEET = "Log"

license_sheet = client.open_by_key(LICENSE_SHEET_ID).worksheet(LICENSE_SHEET)
log_sheet = client.open_by_key(LICENSE_SHEET_ID).worksheet(LOG_SHEET)


def log_action(action, username, changes):
    log_sheet.append_row([
        datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        action,
        username,
        str(changes)
    ])


@app.route("/")
def home():
    return "License API is running!"


@app.before_request
def require_api_key():
    if request.path != '/' and request.headers.get('X-API-KEY') != os.environ.get("API_KEY"):
        return jsonify({"error": "Unauthorized"}), 401


@app.route('/license', methods=['GET'])
def get_license():
    serialnum = request.args.get('serialnum')
    if not serialnum:
        return jsonify({"error": "USB Serial not provided"}), 400

    records = license_sheet.get_all_records()
    for row in records:
        if row["usb_serial"].strip().lower() == serialnum.strip().lower():
            return jsonify(row), 200

    log_action("GET_FAIL", serialnum, "User with given serialnum not found")
    return jsonify({"error": "User not found"}), 404


@app.route('/license', methods=['POST'])
def create_license():
    data = request.get_json()
    required_fields = ['username', 'usb_serial', 'circle', 'lic_type']

    for field in required_fields:
        if field not in data:
            return jsonify({"error": f"Missing field: {field}"}), 400
    
    lic_type = data['lic_type'].strip().upper()
    today = datetime.today()

    if lic_type == "TRIAL":
        valid_till = today + timedelta(days=2)
    elif lic_type == "ALL":
        valid_till = today + timedelta(days=365)
    elif lic_type == "BULKPDF":
        valid_till = today + timedelta(days=180)
    elif lic_type == "SACFA":
        valid_till = today + timedelta(days=90)
    else:
        return jsonify({"error": "Invalid license type"}), 400

    valid_till_str = valid_till.strftime("%d-%m-%Y")

    records = license_sheet.get_all_records()
    for row in records:
        if row["username"].strip().lower() == data["username"].strip().lower():
            return jsonify({"error": "Username already exists"}), 409

    license_sheet.append_row([
        data['username'], data['usb_serial'], data['circle'], valid_till_str, lic_type
    ])
    log_action("CREATE", data['username'], {
        "usb_serial": data['usb_serial'],
        "circle": data['circle'],
        "valid_till": valid_till_str,
        "lic_type": lic_type
    })
    return jsonify({"message": "License created"}), 201


@app.route('/license', methods=['PUT'])
def update_license():
    serialnum = request.args.get('serialnum')
    data = request.get_json()

    if not serialnum:
        return jsonify({"error": "USB serialnum is required"}), 400

    if 'valid_till' in data:
        try:
            datetime.strptime(data['valid_till'], "%d-%m-%Y")
        except ValueError:
            return jsonify({"error": "Invalid date format. Use DD-MM-YYYY"}), 400

    try:
        records = license_sheet.get_all_records()
        matched_row = None
        for idx, row in enumerate(records, start=2):  # 2 = start from row 2
            if row["usb_serial"].strip().lower() == serialnum.strip().lower():
                matched_row = idx
                break

        if not matched_row:
            log_action("UPDATE_FAIL", serialnum, "User with given serial not found")
            return jsonify({"error": "User not found"}), 404

        headers = license_sheet.row_values(1)
        for key, value in data.items():
            if key in headers:
                col_index = headers.index(key) + 1
                license_sheet.update_cell(matched_row, col_index, value)

        log_action("UPDATE", serialnum, data)
        return jsonify({"message": "License updated"}), 200

    except Exception as e:
        log_action("UPDATE_ERROR", serialnum, str(e))
        return jsonify({"error": str(e)}), 500


@app.route('/license', methods=['DELETE'])
def delete_license():
    serialnum = request.args.get('serialnum')
    if not serialnum:
        return jsonify({"error": "USB serialnum is required"}), 400

    try:
        records = license_sheet.get_all_records()
        matched_row = None
        for idx, row in enumerate(records, start=2):
            if row["usb_serial"].strip().lower() == serialnum.strip().lower():
                matched_row = idx
                break

        if not matched_row:
            log_action("DELETE_FAIL", serialnum, "User not found")
            return jsonify({"error": "User not found"}), 404

        license_sheet.delete_rows(matched_row)
        log_action("DELETE", serialnum, {})
        return jsonify({"message": "License deleted"}), 200

    except Exception as e:
        log_action("DELETE_ERROR", serialnum, str(e))
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True)
