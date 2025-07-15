from flask import Flask, request, jsonify
import gspread
from oauth2client.service_account import ServiceAccountCredentials
from datetime import datetime
import os
import json

app = Flask(__name__)

# Use credentials from environment variable
creds_json = os.environ.get("SERVICE_JSON")
creds_dict = json.loads(creds_json)

# Google Sheets setup
scope = ['https://spreadsheets.google.com/feeds', 'https://www.googleapis.com/auth/drive']
creds = ServiceAccountCredentials.from_json_keyfile_dict(creds_dict, scope)
client = gspread.authorize(creds)

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
    username = request.args.get('username')
    if not username:
        return jsonify({"error": "Username not provided"}), 400

    records = license_sheet.get_all_records()
    for row in records:
        if row["username"].strip().lower() == username.strip().lower():
            return jsonify(row), 200

    log_action("GET_FAIL", username, "User not found")
    return jsonify({"error": "User not found"}), 404


@app.route('/license', methods=['POST'])
def create_license():
    data = request.get_json()
    required_fields = ['username', 'circle', 'valid_till', 'lic_type']

    for field in required_fields:
        if field not in data:
            return jsonify({"error": f"Missing field: {field}"}), 400

    try:
        datetime.strptime(data['valid_till'], "%d-%m-%Y")
    except ValueError:
        return jsonify({"error": "Invalid date format. Use DD-MM-YYYY"}), 400

    records = license_sheet.get_all_records()
    for row in records:
        if row["username"].strip().lower() == data["username"].strip().lower():
            return jsonify({"error": "Username already exists"}), 409

    license_sheet.append_row([
        data['username'], data['circle'], data['valid_till'], data['lic_type']
    ])
    log_action("CREATE", data['username'], data)
    return jsonify({"message": "License created"}), 201


@app.route('/license', methods=['PUT'])
def update_license():
    username = request.args.get('username')
    data = request.get_json()

    if not username:
        return jsonify({"error": "Username is required"}), 400

    if 'valid_till' in data:
        try:
            datetime.strptime(data['valid_till'], "%d-%m-%Y")
        except ValueError:
            return jsonify({"error": "Invalid date format. Use DD-MM-YYYY"}), 400

    try:
        records = license_sheet.get_all_records()
        matched_row = None
        for idx, row in enumerate(records, start=2):  # 2 = start from row 2
            if row["username"].strip().lower() == username.strip().lower():
                matched_row = idx
                break

        if not matched_row:
            log_action("UPDATE_FAIL", username, "User not found")
            return jsonify({"error": "User not found"}), 404

        headers = license_sheet.row_values(1)
        for key, value in data.items():
            if key in headers:
                col_index = headers.index(key) + 1
                license_sheet.update_cell(matched_row, col_index, value)

        log_action("UPDATE", username, data)
        return jsonify({"message": "License updated"}), 200

    except Exception as e:
        log_action("UPDATE_ERROR", username, str(e))
        return jsonify({"error": str(e)}), 500


@app.route('/license', methods=['DELETE'])
def delete_license():
    username = request.args.get('username')
    if not username:
        return jsonify({"error": "Username is required"}), 400

    try:
        records = license_sheet.get_all_records()
        matched_row = None
        for idx, row in enumerate(records, start=2):
            if row["username"].strip().lower() == username.strip().lower():
                matched_row = idx
                break

        if not matched_row:
            log_action("DELETE_FAIL", username, "User not found")
            return jsonify({"error": "User not found"}), 404

        license_sheet.delete_rows(matched_row)
        log_action("DELETE", username, {})
        return jsonify({"message": "License deleted"}), 200

    except Exception as e:
        log_action("DELETE_ERROR", username, str(e))
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True)
