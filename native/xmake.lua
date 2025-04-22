add_rules("mode.debug", "mode.release")

set_project("sharpspades")
set_languages("c11")
set_warnings("allextra", "pedantic")

add_requires("enet6 6.1.2")

target("sharpspades", function ()
    set_kind("shared")
    add_files("*.c")
    add_packages("enet6")
end)
