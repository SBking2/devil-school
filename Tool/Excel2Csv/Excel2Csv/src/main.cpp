#include <stdio.h>
#include <xlnt/xlnt.hpp>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <string>

// 判断单元格是否真的有内容
bool cell_has_content(const xlnt::cell& cell)
{
	if (!cell.has_value())
		return false;
	std::string val = cell.to_string();
	for (char c : val)
		if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
			return true;
	return false;
}
void TransToCsv(const std::filesystem::path& excel_path, const std::filesystem::path& csv_path)
{
	xlnt::workbook wb;
	wb.load(excel_path);
	auto& ws = wb.active_sheet();
	std::ofstream csv_file(csv_path.string());
	if (!csv_file.is_open())
		return;
	int row_count = 0;
	int max_col = 0;
	// 第一次遍历找第二行有效列数
	for (auto& row : ws.rows(false))
	{
		++row_count;
		if (row_count == 1) // 跳过第一行注释
			continue;
		if (row_count == 2)
		{
			int col_idx = 0;
			max_col = 0;
			for (auto& cell : row)
			{
				++col_idx;
				if (cell_has_content(cell))
					max_col = col_idx; // 最后一个非空单元格
			}
			break; // 第二行就够了
		}
	}
	row_count = 0; // 重新开始遍历输出 CSV
	for (auto& row : ws.rows(false))
	{
		++row_count;
		if (row_count == 1) // 跳过第一行注释
			continue;
		bool first_cell = true;
		int col_index = 1;
		bool is_valid_row = true;
		for (auto& cell : row)
		{
			if (!first_cell)
				csv_file << "~";
			else
			{
				first_cell = false;
				if(cell_has_content(cell) == false)
				{
					is_valid_row = false;
					break;
				}
			}
			if (cell_has_content(cell))
				csv_file << cell.to_string();
			++col_index;
			if (col_index > max_col)
				break; // 超过第二行真实列数就停止
		}

		if (is_valid_row == false)
			continue;
		// 补齐空列
		//for (; col_index <= max_col; ++col_index)
		//	csv_file << "~";
		csv_file << "\n";
	}
	csv_file.close();
	std::cout << "Output: " << csv_path.string() << std::endl;
}
int main(int argc, char* argv[])
{
	std::ifstream cfg("config.txt");
	std::string project_path_str;
	std::string excel_path_str;
	std::string csv_path_str;
	if (cfg.is_open())
	{
		std::getline(cfg, project_path_str);
		std::getline(cfg, excel_path_str);
		std::getline(cfg, csv_path_str);
	}
	else
		return -1;
	std::filesystem::path excel_path_f(project_path_str);
	excel_path_f /= excel_path_str;
	std::filesystem::path csv_path_f(project_path_str);
	csv_path_f /= csv_path_str;
	auto e_valid = std::filesystem::exists(excel_path_f) && std::filesystem::is_directory(excel_path_f);
	auto c_valid = std::filesystem::exists(csv_path_f) && std::filesystem::is_directory(csv_path_f);
	if (e_valid == false || c_valid == false)
		return -1;
	printf("Excel Path: %s\n", excel_path_f.string().c_str());
	printf("Csv Path: %s\n", csv_path_f.string().c_str());
	printf("\n");
	//清空旧文件夹
	std::vector<std::filesystem::path> remove_files;
	for (const auto& entry : std::filesystem::recursive_directory_iterator(csv_path_f))
		remove_files.emplace_back(entry.path());
	for (const auto& path : remove_files)
		std::filesystem::remove_all(path);
	for (const auto& entry : std::filesystem::recursive_directory_iterator(excel_path_f))
	{
		auto& extension = entry.path().extension();
		if (entry.is_regular_file() && extension == ".xlsx")
		{
			std::filesystem::path csv_file_path = csv_path_f / entry.path().stem();
			csv_file_path += ".csv";
			TransToCsv(entry.path(), csv_file_path);
		}
	}
	return 0;
}