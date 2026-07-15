using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class EncodingFixer
{
    static readonly Dictionary<int, byte> Win1252Reverse = new Dictionary<int, byte> {
        {0x20AC, 0x80}, {0x201A, 0x82}, {0x0192, 0x83}, {0x201E, 0x84},
        {0x2026, 0x85}, {0x2020, 0x86}, {0x2021, 0x87}, {0x02C6, 0x88},
        {0x2030, 0x89}, {0x0160, 0x8A}, {0x2039, 0x8B}, {0x0152, 0x8C},
        {0x017D, 0x8E}, {0x2018, 0x91}, {0x2019, 0x92}, {0x201C, 0x93},
        {0x201D, 0x94}, {0x2022, 0x95}, {0x2013, 0x96}, {0x2014, 0x97},
        {0x02DC, 0x98}, {0x2122, 0x99}, {0x0161, 0x9A}, {0x203A, 0x9B},
        {0x0153, 0x9C}, {0x017E, 0x9E}, {0x0178, 0x9F}
    };

    public static string Fix(string corrupted)
    {
        var bytes = new List<byte>();
        foreach (char c in corrupted)
        {
            int cp = c;
            if (cp <= 0x7F)
                bytes.Add((byte)cp);
            else if (Win1252Reverse.ContainsKey(cp))
                bytes.Add(Win1252Reverse[cp]);
            else if (cp <= 0xFF)
                bytes.Add((byte)cp);
            else
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(new char[] { c });
                bytes.AddRange(utf8);
            }
        }
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }

    static void Main()
    {
        string conn = @"Server=.\SQLEXPRESS;Database=QuanLyThuVien;Integrated Security=true;Encrypt=false";
        using var c = new SqlConnection(conn);
        c.Open();

        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Fix DocGia
        Console.WriteLine("=== DOC GIA ===");
        var cmd = new SqlCommand("SELECT MaDG, HoTen FROM DocGia", c);
        var reader = cmd.ExecuteReader();
        var dgRecords = new List<(int id, string name)>();
        while (reader.Read()) dgRecords.Add((reader.GetInt32(0), reader.GetString(1)));
        reader.Close();

        foreach (var (id, name) in dgRecords)
        {
            string fixed_name = Fix(name);
            Console.WriteLine($"DG {id}: {fixed_name}");
            using var update = new SqlCommand("UPDATE DocGia SET HoTen=@ten WHERE MaDG=@id", c);
            update.Parameters.AddWithValue("@ten", fixed_name);
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
        }

        // Fix Sach
        Console.WriteLine("\n=== SACH ===");
        cmd = new SqlCommand("SELECT MaSach, TenSach FROM Sach", c);
        reader = cmd.ExecuteReader();
        var sachRecords = new List<(int id, string name)>();
        while (reader.Read()) sachRecords.Add((reader.GetInt32(0), reader.GetString(1)));
        reader.Close();

        foreach (var (id, name) in sachRecords)
        {
            string fixed_name = Fix(name);
            Console.WriteLine($"Sach {id}: {fixed_name}");
            using var update = new SqlCommand("UPDATE Sach SET TenSach=@ten WHERE MaSach=@id", c);
            update.Parameters.AddWithValue("@ten", fixed_name);
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
        }

        // Fix NhanVien
        Console.WriteLine("\n=== NHAN VIEN ===");
        cmd = new SqlCommand("SELECT MaNV, HoTen FROM NhanVien", c);
        reader = cmd.ExecuteReader();
        var nvRecords = new List<(int id, string name)>();
        while (reader.Read()) nvRecords.Add((reader.GetInt32(0), reader.GetString(1)));
        reader.Close();

        foreach (var (id, name) in nvRecords)
        {
            string fixed_name = Fix(name);
            Console.WriteLine($"NV {id}: {fixed_name}");
            using var update = new SqlCommand("UPDATE NhanVien SET HoTen=@ten WHERE MaNV=@id", c);
            update.Parameters.AddWithValue("@ten", fixed_name);
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
        }

        // Fix TheLoai
        Console.WriteLine("\n=== THE LOAI ===");
        cmd = new SqlCommand("SELECT MaTL, TenTheLoai FROM TheLoai", c);
        reader = cmd.ExecuteReader();
        var tlRecords = new List<(int id, string name)>();
        while (reader.Read()) tlRecords.Add((reader.GetInt32(0), reader.GetString(1)));
        reader.Close();

        foreach (var (id, name) in tlRecords)
        {
            string fixed_name = Fix(name);
            Console.WriteLine($"TL {id}: {fixed_name}");
            using var update = new SqlCommand("UPDATE TheLoai SET TenTheLoai=@ten WHERE MaTL=@id", c);
            update.Parameters.AddWithValue("@ten", fixed_name);
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
        }

        // Fix TacGia
        Console.WriteLine("\n=== TAC GIA ===");
        cmd = new SqlCommand("SELECT MaTG, TenTG FROM TacGia", c);
        reader = cmd.ExecuteReader();
        var tgRecords = new List<(int id, string name)>();
        while (reader.Read()) tgRecords.Add((reader.GetInt32(0), reader.GetString(1)));
        reader.Close();

        foreach (var (id, name) in tgRecords)
        {
            string fixed_name = Fix(name);
            Console.WriteLine($"TG {id}: {fixed_name}");
            using var update = new SqlCommand("UPDATE TacGia SET TenTG=@ten WHERE MaTG=@id", c);
            update.Parameters.AddWithValue("@ten", fixed_name);
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
        }

        // Fix NhaXuatBan
        Console.WriteLine("\n=== NHA XUAT BAN ===");
        cmd = new SqlCommand("SELECT MaNXB, TenNXB FROM NhaXuatBan", c);
        reader = cmd.ExecuteReader();
        var nxbRecords = new List<(int id, string name)>();
        while (reader.Read()) nxbRecords.Add((reader.GetInt32(0), reader.GetString(1)));
        reader.Close();

        foreach (var (id, name) in nxbRecords)
        {
            string fixed_name = Fix(name);
            Console.WriteLine($"NXB {id}: {fixed_name}");
            using var update = new SqlCommand("UPDATE NhaXuatBan SET TenNXB=@ten WHERE MaNXB=@id", c);
            update.Parameters.AddWithValue("@ten", fixed_name);
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
        }

        Console.WriteLine("\nDone! All tables fixed.");
    }
}
