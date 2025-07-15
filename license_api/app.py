from flask import Flask, request, jsonify
import gspread
from oauth2client.service_account import ServiceAccountCredentials
from datetime import datetime

app = Flask(__name__)

# Google Sheets setup
scope = ['https://spreadsheets.google.com/feeds', 'https://www.googleapis.com/auth/drive']
creds = ServiceAccountCredentials.from_json_keyfile_name('licenser.json', scope)
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


@app.route('/license', methods=['GET'])
def get_license():
    username = request.args.get('username')
    if not username:
        return jsonify({"error": "Username not provided"}), 400

    records = license_sheet.get_all_records()
    for row in records:
        if row["username"].strip().lower() == username.strip().lower():
            return jsonify(row), 200
    return jsonify({"error": "User not found"}), 404


@app.route('/license', methods=['POST'])
def create_license():
    data = request.get_json()
    required_fields = ['username', 'circle', 'valid_till', 'lic_type']

    for field in required_fields:
        if field not in data:
            return jsonify({"error": f"Missing field: {field}"}), 400

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

    records = license_sheet.get_all_records()
    cell = license_sheet.find(username)
    if not cell:
        return jsonify({"error": "User not found"}), 404

    row_index = cell.row
    headers = license_sheet.row_values(1)

    # Update each provided field
    for key, value in data.items():
        if key in headers:
            col_index = headers.index(key) + 1
            license_sheet.update_cell(row_index, col_index, value)

    log_action("UPDATE", username, data)
    return jsonify({"message": "License updated"}), 200


@app.route('/license', methods=['DELETE'])
def delete_license():
    username = request.args.get('username')
    if not username:
        return jsonify({"error": "Username is required"}), 400

    try:
        cell = license_sheet.find(username)
        if not cell:
            return jsonify({"error": "User not found"}), 404
        license_sheet.delete_rows(cell.row)
        log_action("DELETE", username, {})
        return jsonify({"message": "License deleted"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True)
    