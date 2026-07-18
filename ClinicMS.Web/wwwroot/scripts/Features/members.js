// Members JS
var members = [
    {id:1,name:'Mahmoud Samir',phone:'+20 100 111 2222',gender:'Male',shift:'Morning',pkg:'Pro Monthly',subStatus:'Active',branch:'Main Branch',status:'Active',email:'mahmoud.s@email.com',dob:'1990-03-10',nid:'29003100012345'},
    {id:2,name:'Rana Youssef',phone:'+20 101 222 3333',gender:'Female',shift:'Evening',pkg:'Basic Monthly',subStatus:'Active',branch:'Main Branch',status:'Active',email:'rana.y@email.com',dob:'1995-07-22',nid:'29507220023456'},
    {id:3,name:'Tamir Ahmed',phone:'+20 102 333 4444',gender:'Male',shift:'Afternoon',pkg:'Elite Monthly',subStatus:'Expiring',branch:'North Branch',status:'Active',email:'tamir.a@email.com',dob:'1988-11-05',nid:'28811050034567'},
    {id:4,name:'Noha Khalil',phone:'+20 103 444 5555',gender:'Female',shift:'Morning',pkg:'Annual',subStatus:'Active',branch:'South Branch',status:'Active',email:'noha.k@email.com',dob:'1992-04-18',nid:'29204180045678'},
    {id:5,name:'Adel Hassan',phone:'+20 104 555 6666',gender:'Male',shift:'Evening',pkg:'Pro 6-Month',subStatus:'Active',branch:'Main Branch',status:'Active',email:'adel.h@email.com',dob:'1985-09-30',nid:'28509300056789'},
    {id:6,name:'Samar Ibrahim',phone:'+20 105 666 7777',gender:'Female',shift:'Night',pkg:'Basic Monthly',subStatus:'Expired',branch:'East Branch',status:'Inactive',email:'samar.i@email.com',dob:'1997-01-14',nid:'29701140067890'},
    {id:7,name:'Karim Mostafa',phone:'+20 106 777 8888',gender:'Male',shift:'Morning',pkg:'Pro Monthly',subStatus:'Active',branch:'Main Branch',status:'Active',email:'karim.m@email.com',dob:'1993-06-25',nid:'29306250078901'},
    {id:8,name:'Amira Samy',phone:'+20 107 888 9999',gender:'Female',shift:'Afternoon',pkg:'Basic 3-Month',subStatus:'Active',branch:'North Branch',status:'Active',email:'amira.s@email.com',dob:'1996-12-08',nid:'29612080089012'},
    {id:9,name:'Ziad Omar',phone:'+20 108 999 0000',gender:'Male',shift:'Evening',pkg:'Elite Monthly',subStatus:'Expiring',branch:'Main Branch',status:'Active',email:'ziad.o@email.com',dob:'1991-08-17',nid:'29108170090123'},
    {id:10,name:'Dalia Nasser',phone:'+20 109 000 1111',gender:'Female',shift:'Morning',pkg:'Annual',subStatus:'Active',branch:'South Branch',status:'Active',email:'dalia.n@email.com',dob:'1989-02-28',nid:'28902280001234'},
    {id:11,name:'Bassem Fawzy',phone:'+20 110 111 2222',gender:'Male',shift:'Night',pkg:'Pro Monthly',subStatus:'Active',branch:'East Branch',status:'Active',email:'bassem.f@email.com',dob:'1994-05-06',nid:'29405060012345'},
    {id:12,name:'Yasmine Ali',phone:'+20 111 222 3333',gender:'Female',shift:'Afternoon',pkg:'Basic Monthly',subStatus:'Expired',branch:'Main Branch',status:'Inactive',email:'yasmine.a@email.com',dob:'1998-10-15',nid:'29810150023456'},
    {id:13,name:'Sherif Mahmoud',phone:'+20 112 333 4444',gender:'Male',shift:'Morning',pkg:'Pro 6-Month',subStatus:'Active',branch:'North Branch',status:'Active',email:'sherif.m@email.com',dob:'1987-07-20',nid:'28707200034567'},
    {id:14,name:'Reem Hassan',phone:'+20 113 444 5555',gender:'Female',shift:'Evening',pkg:'Elite Monthly',subStatus:'Active',branch:'Main Branch',status:'Active',email:'reem.h@email.com',dob:'1993-03-12',nid:'29303120045678'},
    {id:15,name:'Nabil Ibrahim',phone:'+20 114 555 6666',gender:'Male',shift:'Morning',pkg:'Annual',subStatus:'Active',branch:'South Branch',status:'Active',email:'nabil.i@email.com',dob:'1983-11-30',nid:'28311300056789'},
    {id:16,name:'Heba Mostafa',phone:'+20 115 666 7777',gender:'Female',shift:'Afternoon',pkg:'Basic Monthly',subStatus:'Expiring',branch:'East Branch',status:'Active',email:'heba.m@email.com',dob:'1996-06-04',nid:'29606040067890'},
    {id:17,name:'Wael Salem',phone:'+20 116 777 8888',gender:'Male',shift:'Evening',pkg:'Pro Monthly',subStatus:'Active',branch:'Main Branch',status:'Active',email:'wael.s@email.com',dob:'1990-09-19',nid:'29009190078901'},
    {id:18,name:'Mona Youssef',phone:'+20 117 888 9999',gender:'Female',shift:'Night',pkg:'Basic 3-Month',subStatus:'Active',branch:'North Branch',status:'Active',email:'mona.y@email.com',dob:'1995-02-26',nid:'29502260089012'},
    {id:19,name:'Tamer Lotfi',phone:'+20 118 999 0000',gender:'Male',shift:'Morning',pkg:'Elite Monthly',subStatus:'Expired',branch:'Main Branch',status:'Inactive',email:'tamer.l@email.com',dob:'1986-04-11',nid:'28604110090123'},
    {id:20,name:'Aya Khalid',phone:'+20 119 000 1111',gender:'Female',shift:'Afternoon',pkg:'Annual',subStatus:'Active',branch:'South Branch',status:'Active',email:'aya.k@email.com',dob:'1997-08-23',nid:'29708230001234'}
];

// Active session user — branch is auto-assigned to new members
var ACTIVE_USER = (typeof ACTIVE_USER !== 'undefined') ? ACTIVE_USER : { name: 'Ahmed Hassan', branch: 'Main Branch', role: 'Admin' };

var currentPage=1, perPage=8, searchQuery='', pkgF='', statusF='', branchF='', editingId=null;
var avatarColors=['#0d9488','#2563eb','#7c3aed','#dc2626','#d97706','#16a34a'];
function initials(n){return n.split(' ').map(p=>p[0]).join('').toUpperCase().slice(0,2);}
function avatarColor(i){return avatarColors[i%avatarColors.length];}

var subStatusBadge={'Active':'gp-badge-green','Expiring':'gp-badge-yellow','Expired':'gp-badge-red','Cancelled':'gp-badge-gray'};

function filtered(){
    var q=searchQuery.toLowerCase();
    return members.filter(m=>(!q||m.name.toLowerCase().includes(q)||m.phone.includes(q))&&(!pkgF||m.pkg===pkgF)&&(!statusF||m.status===statusF)&&(!branchF||m.branch===branchF));
}

function renderTable(){
    var data=filtered();var total=data.length;
    var pages=Math.ceil(total/perPage)||1;if(currentPage>pages)currentPage=1;
    var slice=data.slice((currentPage-1)*perPage,currentPage*perPage);
    document.getElementById('membersTableBody').innerHTML=slice.length?slice.map((m,i)=>{
        var idx=members.indexOf(m);
        return `<tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><div style="display:flex;align-items:center;gap:10px;">
                <div class="gp-avatar" style="background:${avatarColor(idx)}">${initials(m.name)}</div>
                <span style="font-weight:700;color:#1e293b;">${m.name}</span>
            </div></td>
            <td>${m.phone}</td>
            <td><span class="gp-badge ${m.gender==='Male'?'gp-badge-blue':'gp-badge-purple'}">${m.gender}</span></td>
            <td>${m.shift}</td>
            <td><span style="font-size:.82rem;font-weight:600;">${m.pkg}</span></td>
            <td><span class="gp-badge ${subStatusBadge[m.subStatus]||'gp-badge-gray'}">${m.subStatus}</span></td>
            <td>${m.branch}</td>
            <td><span class="gp-badge ${m.status==='Active'?'gp-badge-green':'gp-badge-red'}">${m.status}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-view" onclick="viewMember(${m.id})"><i class="ri-eye-line"></i></button>
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${m.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteMember(${m.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`;}).join(''):'<tr><td colspan="10" class="text-center py-4 text-muted">No members found</td></tr>';
    document.getElementById('pageInfo').textContent=`Showing ${slice.length} of ${total}`;
    var btns=document.getElementById('pageBtns');btns.innerHTML='';
    for(var p=1;p<=pages;p++){var btn=document.createElement('button');btn.className='gp-page-btn'+(p===currentPage?' active':'');btn.textContent=p;btn.onclick=(function(pp){return function(){currentPage=pp;renderTable();};})(p);btns.appendChild(btn);}
    document.getElementById('statTotal').textContent=members.length;
    document.getElementById('statActive').textContent=members.filter(m=>m.subStatus==='Active').length;
    document.getElementById('statExpiring').textContent=members.filter(m=>m.subStatus==='Expiring').length;
    document.getElementById('statExpired').textContent=members.filter(m=>m.subStatus==='Expired').length;
}

function setMemberBranchDisplay(branch) {
    var display = document.getElementById('memberBranchDisplay');
    var hidden  = document.getElementById('fBranch');
    if (display) display.textContent = branch;
    if (hidden)  hidden.value = branch;
}

function openModal(id){
    editingId=id||null;
    document.getElementById('modalTitle').textContent=id?'Edit Member':'Add Member';
    if(id){
        var m=members.find(x=>x.id===id);
        document.getElementById('fName').value=m.name;
        document.getElementById('fPhone').value=m.phone;
        document.getElementById('fEmail').value=m.email;
        document.getElementById('fGender').value=m.gender;
        document.getElementById('fPackage').value=m.pkg;
        document.getElementById('fShift').value=m.shift;
        document.getElementById('fStatus').checked=m.status==='Active';
        setMemberBranchDisplay(m.branch);
    } else {
        ['fName','fPhone','fEmail','fDOB','fNID'].forEach(f=>document.getElementById(f).value='');
        document.getElementById('fStatus').checked=true;
        setMemberBranchDisplay(ACTIVE_USER.branch);
    }
    new bootstrap.Modal(document.getElementById('memberModal')).show();
}

function saveMember(){
    var name=document.getElementById('fName').value.trim();
    if(!name){toastr.error('Name required');return;}
    var data={id:editingId||Date.now(),name,phone:document.getElementById('fPhone').value,email:document.getElementById('fEmail').value,gender:document.getElementById('fGender').value,shift:document.getElementById('fShift').value,pkg:document.getElementById('fPackage').value,subStatus:'Active',branch:document.getElementById('fBranch').value,status:document.getElementById('fStatus').checked?'Active':'Inactive'};
    if(editingId){var idx=members.findIndex(x=>x.id===editingId);members[idx]={...members[idx],...data};toastr.success('Updated');}
    else{members.push(data);toastr.success('Member added');}
    bootstrap.Modal.getInstance(document.getElementById('memberModal')).hide();
    renderTable();
}

function viewMember(id){
    var m=members.find(x=>x.id===id);
    var idx=members.indexOf(m);
    document.getElementById('memberProfileContent').innerHTML=`
        <div style="text-align:center;padding:16px 0;">
            <div style="width:70px;height:70px;border-radius:16px;background:${avatarColor(idx)};display:inline-flex;align-items:center;justify-content:center;font-size:1.5rem;font-weight:800;color:#fff;margin-bottom:10px;">${initials(m.name)}</div>
            <div style="font-size:1.1rem;font-weight:800;color:#1e293b;">${m.name}</div>
            <div style="font-size:.82rem;color:#64748b;">${m.pkg} · ${m.branch}</div>
        </div>
        <div style="display:flex;flex-direction:column;gap:8px;">
            <div style="display:flex;align-items:center;gap:10px;padding:10px 14px;border-radius:10px;background:#f8fafc;"><i class="ri-phone-line" style="color:#0d9488;width:18px;"></i><div><div style="font-size:.7rem;font-weight:700;color:#94a3b8;text-transform:uppercase;">Phone</div><div style="font-size:.875rem;font-weight:600;">${m.phone}</div></div></div>
            <div style="display:flex;align-items:center;gap:10px;padding:10px 14px;border-radius:10px;background:#f8fafc;"><i class="ri-user-line" style="color:#0d9488;width:18px;"></i><div><div style="font-size:.7rem;font-weight:700;color:#94a3b8;text-transform:uppercase;">Gender</div><div style="font-size:.875rem;font-weight:600;">${m.gender}</div></div></div>
            <div style="display:flex;align-items:center;gap:10px;padding:10px 14px;border-radius:10px;background:#f8fafc;"><i class="ri-time-line" style="color:#0d9488;width:18px;"></i><div><div style="font-size:.7rem;font-weight:700;color:#94a3b8;text-transform:uppercase;">Shift</div><div style="font-size:.875rem;font-weight:600;">${m.shift}</div></div></div>
            <div style="display:flex;align-items:center;gap:10px;padding:10px 14px;border-radius:10px;background:#f8fafc;"><i class="ri-vip-crown-line" style="color:#0d9488;width:18px;"></i><div><div style="font-size:.7rem;font-weight:700;color:#94a3b8;text-transform:uppercase;">Subscription</div><div style="font-size:.875rem;font-weight:600;">${m.subStatus}</div></div></div>
        </div>`;
    new bootstrap.Modal(document.getElementById('viewMemberModal')).show();
}

function deleteMember(id){if(!confirm('Delete?'))return;members=members.filter(m=>m.id!==id);toastr.success('Deleted');renderTable();}
function handleSearch(v){searchQuery=v;currentPage=1;renderTable();}
function handleFilter(){pkgF=document.getElementById('pkgFilter').value;statusF=document.getElementById('statusFilter').value;branchF=document.getElementById('branchFilter').value;currentPage=1;renderTable();}
document.addEventListener('DOMContentLoaded',renderTable);

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
// Re-run on each modal open
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
