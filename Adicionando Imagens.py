import pyodbc
from PIL import Image
from io import BytesIO

connection_string = "Driver={ODBC Driver 17 for SQL Server};Server=WIN-NRSBVRMVO1H\SQLEXPRESS;Database=FastOrder;Trusted_Connection=yes;"
conn = pyodbc.connect(connection_string)
cursor = conn.cursor()

def create_image(color):
    img = Image.new('RGB', (360, 400), color=color)
    return img

def insert_image(image_data):
    sql = "INSERT INTO Imagens (Imagem, DataUpload, Tamanho) VALUES (?, GETDATE(), ?)"
    size = len(image_data)
    cursor.execute(sql, (image_data, size))
    conn.commit()


for i in range(90000):
    red_value = (i * 5) % 256
    green_value = (i * 3) % 256
    blue_value = (i * 2) % 256
    color = (red_value, green_value, blue_value)

    img = create_image(color)

    buffer = BytesIO()
    img.save(buffer, format='PNG')
    image_data = buffer.getvalue()

    insert_image(image_data)

print("Funcionou!")

cursor.close()
conn.close()